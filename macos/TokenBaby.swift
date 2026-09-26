import AppKit
import Foundation

struct QuotaWindow {
    let remaining: Double?
    let durationMinutes: Int?
    let resetsAt: Date?
}

struct QuotaSnapshot {
    let fiveHour: QuotaWindow?
    let sevenDay: QuotaWindow?
    let fetchedAt: Date
}

final class CodexClient: @unchecked Sendable {
    var onSnapshot: ((QuotaSnapshot) -> Void)?
    var onStatus: ((String) -> Void)?

    private let queue = DispatchQueue(label: "tokenbaby.codex")
    private var process: Process?
    private var input: FileHandle?
    private var nextID = 3
    private var buffer = Data()
    private var lastRequest = Date.distantPast

    func refresh(force: Bool = true) {
        queue.async { [weak self] in
            guard let self else { return }
            if self.process?.isRunning != true { self.start(); return }
            if !force, Date().timeIntervalSince(self.lastRequest) < 5 { return }
            self.lastRequest = Date()
            self.nextID += 1
            self.send(["method": "account/rateLimits/read", "id": self.nextID,
                       "params": ["excludeResetCreditDetails": true]])
        }
    }

    private func start() {
        guard let executable = findCodex() else {
            status("找不到 Codex。请安装 Codex 桌面版或 CLI，或设置 TOKENBABY_CODEX_PATH。")
            return
        }
        let process = Process()
        let stdinPipe = Pipe(), stdoutPipe = Pipe(), stderrPipe = Pipe()
        process.executableURL = URL(fileURLWithPath: executable)
        process.arguments = ["app-server"]
        process.standardInput = stdinPipe
        process.standardOutput = stdoutPipe
        process.standardError = stderrPipe
        var environment = ProcessInfo.processInfo.environment
        if environment["CODEX_HOME"] == nil {
            environment["CODEX_HOME"] = FileManager.default.homeDirectoryForCurrentUser
                .appendingPathComponent(".codex").path
        }
        process.environment = environment
        stdoutPipe.fileHandleForReading.readabilityHandler = { [weak self] handle in
            let data = handle.availableData
            if !data.isEmpty { self?.consume(data) }
        }
        // Keep stderr drained so a verbose server cannot block on a full pipe.
        stderrPipe.fileHandleForReading.readabilityHandler = { handle in _ = handle.availableData }
        process.terminationHandler = { [weak self] _ in self?.status("Codex 连接中断，稍后重试。") }
        do {
            try process.run()
            self.process = process
            self.input = stdinPipe.fileHandleForWriting
            send(["method": "initialize", "id": 1, "params": ["clientInfo": [
                "name": "tokenbaby", "title": "TokenBaby", "version": "0.2.0-macos"]]])
            send(["method": "initialized", "params": [:] as [String: Any]])
            send(["method": "account/read", "id": 2, "params": ["refreshToken": false]])
            lastRequest = Date()
            send(["method": "account/rateLimits/read", "id": 3,
                  "params": ["excludeResetCreditDetails": true]])
            status("正在读取 Codex 额度…")
        } catch {
            status("无法启动 Codex：\(error.localizedDescription)")
        }
    }

    private func send(_ object: [String: Any]) {
        guard JSONSerialization.isValidJSONObject(object),
              var data = try? JSONSerialization.data(withJSONObject: object) else { return }
        data.append(0x0A)
        try? input?.write(contentsOf: data)
    }

    private func consume(_ data: Data) {
        queue.async { [weak self] in
            guard let self else { return }
            self.buffer.append(data)
            while let newline = self.buffer.firstIndex(of: 0x0A) {
                let line = self.buffer[..<newline]
                self.buffer.removeSubrange(...newline)
                self.handle(Data(line))
            }
        }
    }

    private func handle(_ data: Data) {
        guard let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return }
        if json["method"] as? String == "account/rateLimits/updated" {
            refresh(force: false)
            return
        }
        guard let id = number(json["id"]).map(Int.init) else { return }
        if let error = json["error"] as? [String: Any], id >= 2 {
            status("额度读取失败：\(error["message"] as? String ?? "请检查 Codex 登录状态")")
            return
        }
        guard let result = json["result"] as? [String: Any] else { return }
        if id == 2 {
            let type = (result["account"] as? [String: Any])?["type"] as? String
            if type == nil { status("请先在 Codex 登录 ChatGPT 账号。") }
            else if type == "apiKey" { status("当前是 API Key 登录；请改用 ChatGPT 账号登录 Codex。") }
            return
        }
        guard id >= 3 else { return }
        publish(parseSnapshot(result))
    }

    private func publish(_ snapshot: QuotaSnapshot?) {
        guard let snapshot else { status("Codex 未返回套餐额度，请使用 ChatGPT 账号登录 Codex。"); return }
        DispatchQueue.main.async { [weak self] in self?.onSnapshot?(snapshot); self?.onStatus?("已连接") }
    }

    private func status(_ text: String) {
        DispatchQueue.main.async { [weak self] in self?.onStatus?(text) }
    }

    private func parseSnapshot(_ result: [String: Any]) -> QuotaSnapshot? {
        var bucket: [String: Any]?
        if let groups = result["rateLimitsByLimitId"] as? [String: Any] {
            bucket = groups["codex"] as? [String: Any] ?? groups.values.compactMap { $0 as? [String: Any] }.first
        }
        bucket = bucket ?? result["rateLimits"] as? [String: Any]
        bucket = bucket ?? ((result["primary"] != nil || result["secondary"] != nil) ? result : nil)
        guard let bucket else { return nil }
        let primary = parseWindow(bucket["primary"] as? [String: Any])
        let secondary = parseWindow(bucket["secondary"] as? [String: Any])
        let windows = [primary, secondary].compactMap { $0 }
        let five = windows.first { $0.durationMinutes == 300 } ?? (primary?.durationMinutes == nil ? primary : nil)
        let seven = windows.first { $0.durationMinutes == 10_080 } ?? (secondary?.durationMinutes == nil ? secondary : nil)
        return QuotaSnapshot(fiveHour: five, sevenDay: seven, fetchedAt: Date())
    }

    private func parseWindow(_ map: [String: Any]?) -> QuotaWindow? {
        guard let map else { return nil }
        let used = number(map["usedPercent"])
        let remaining = used.map { max(0, min(100, 100 - $0)) }
        let duration = number(map["windowDurationMins"]).map(Int.init)
        let reset = number(map["resetsAt"]).map { Date(timeIntervalSince1970: $0) }
        return QuotaWindow(remaining: remaining, durationMinutes: duration, resetsAt: reset)
    }

    private func number(_ value: Any?) -> Double? {
        if let n = value as? NSNumber { return n.doubleValue }
        if let s = value as? String { return Double(s) }
        return nil
    }

    private func findCodex() -> String? {
        let env = ProcessInfo.processInfo.environment
        var candidates: [String] = []
        if let override = env["TOKENBABY_CODEX_PATH"] { candidates.append(override) }
        candidates += (env["PATH"] ?? "").split(separator: ":").map { "\($0)/codex" }
        let home = FileManager.default.homeDirectoryForCurrentUser.path
        candidates += ["/opt/homebrew/bin/codex", "/usr/local/bin/codex", "\(home)/.local/bin/codex",
                       "\(home)/.npm-global/bin/codex", "\(home)/Library/pnpm/codex"]
        return candidates.first { FileManager.default.isExecutableFile(atPath: $0) }
    }

    func stop() {
        queue.sync {
            process?.terminationHandler = nil
            if process?.isRunning == true { process?.terminate() }
            process = nil; input = nil
        }
    }
}

final class PetView: NSView {
    var clicked: (() -> Void)?
    private var idleName = "pet-high"
    private var actionName = "pet-laugh"
    private var image: NSImage?
    private var mouseDownLocation: NSPoint?
    private var pendingRevert: DispatchWorkItem?

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        show(named: idleName)
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    func apply(remaining: Double?) {
        if let remaining, remaining <= 20 {
            idleName = "pet-low"; actionName = "pet-cry"
        } else if let remaining, remaining <= 70 {
            idleName = "pet-mid"; actionName = "pet-glance"
        } else {
            idleName = "pet-high"; actionName = "pet-laugh"
        }
        show(named: idleName)
    }

    func playClickAnimation() {
        pendingRevert?.cancel()
        show(named: actionName)
        let work = DispatchWorkItem { [weak self] in
            guard let self else { return }
            self.show(named: self.idleName)
        }
        pendingRevert = work
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.35, execute: work)
    }

    private func show(named name: String) {
        let bundled = Bundle.main.url(forResource: name, withExtension: "png")
        let development = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
            .appendingPathComponent("assets/\(name).png")
        image = NSImage(contentsOf: bundled ?? development)
        needsDisplay = true
    }

    override var mouseDownCanMoveWindow: Bool { true }
    override func mouseDown(with event: NSEvent) {
        mouseDownLocation = event.locationInWindow
        super.mouseDown(with: event)
    }
    override func mouseUp(with event: NSEvent) {
        if let start = mouseDownLocation,
           hypot(event.locationInWindow.x - start.x, event.locationInWindow.y - start.y) < 5 {
            playClickAnimation()
            clicked?()
        }
        mouseDownLocation = nil
    }
    override func draw(_ dirtyRect: NSRect) {
        super.draw(dirtyRect)
        guard let image else { return }
        let scale = min(bounds.width / image.size.width, bounds.height / image.size.height)
        let size = NSSize(width: image.size.width * scale, height: image.size.height * scale)
        let rect = NSRect(x: bounds.midX - size.width / 2, y: bounds.midY - size.height / 2,
                          width: size.width, height: size.height)
        image.draw(in: rect, from: .zero, operation: .sourceOver, fraction: 1,
                   respectFlipped: true, hints: [.interpolation: NSImageInterpolation.high])
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    private let client = CodexClient()
    private var statusItem: NSStatusItem!
    private var petWindow: NSWindow!
    private var panel: NSPopover!
    private let statusLabel = NSTextField(labelWithString: "正在连接…")
    private let fiveLabel = NSTextField(labelWithString: "5 小时：--")
    private let sevenLabel = NSTextField(labelWithString: "7 天：--")
    private let updatedLabel = NSTextField(labelWithString: "")
    private let petView = PetView(frame: NSRect(x: 0, y: 0, width: 170, height: 245))

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)
        createPet(); createMenu(); createPanel()
        client.onStatus = { [weak self] in self?.statusLabel.stringValue = $0 }
        client.onSnapshot = { [weak self] in self?.apply($0) }
        client.refresh()
        Timer.scheduledTimer(withTimeInterval: 60, repeats: true) { [weak self] _ in self?.client.refresh() }
    }

    private func createPet() {
        petWindow = NSWindow(contentRect: NSRect(x: 100, y: 100, width: 170, height: 245),
                             styleMask: [.borderless], backing: .buffered, defer: false)
        petWindow.isOpaque = false; petWindow.backgroundColor = .clear; petWindow.level = .floating
        petWindow.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        petWindow.isMovableByWindowBackground = true; petWindow.hasShadow = true
        petView.clicked = { [weak self] in self?.togglePanel() }
        petWindow.contentView = petView
        if let saved = UserDefaults.standard.string(forKey: "pet.origin") {
            petWindow.setFrameOrigin(NSPointFromString(saved))
        } else if let screen = NSScreen.main { petWindow.setFrameOrigin(.init(x: screen.visibleFrame.maxX - 195, y: screen.visibleFrame.minY + 40)) }
        NotificationCenter.default.addObserver(forName: NSWindow.didMoveNotification, object: petWindow, queue: .main) { note in
            guard let w = note.object as? NSWindow else { return }
            UserDefaults.standard.set(NSStringFromPoint(w.frame.origin), forKey: "pet.origin")
        }
        petWindow.orderFrontRegardless()
    }

    private func createMenu() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.title = "🐣 --"
        let menu = NSMenu()
        menu.addItem(withTitle: "显示额度", action: #selector(togglePanel), keyEquivalent: "")
        menu.addItem(withTitle: "显示 / 隐藏宠物", action: #selector(togglePet), keyEquivalent: "")
        menu.addItem(withTitle: "刷新额度", action: #selector(refresh), keyEquivalent: "r")
        menu.addItem(.separator())
        menu.addItem(withTitle: "退出 TokenBaby", action: #selector(quit), keyEquivalent: "q")
        for item in menu.items { item.target = self }
        statusItem.menu = menu
    }

    private func createPanel() {
        let stack = NSStackView(views: [statusLabel, fiveLabel, sevenLabel, updatedLabel])
        stack.orientation = .vertical; stack.alignment = .leading; stack.spacing = 9
        let refresh = NSButton(title: "立即刷新", target: self, action: #selector(refresh)); stack.addArrangedSubview(refresh)
        let container = NSView(frame: NSRect(x: 0, y: 0, width: 280, height: 160))
        stack.translatesAutoresizingMaskIntoConstraints = false; container.addSubview(stack)
        NSLayoutConstraint.activate([stack.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 18), stack.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -18), stack.topAnchor.constraint(equalTo: container.topAnchor, constant: 18)])
        let controller = NSViewController(); controller.view = container
        panel = NSPopover(); panel.contentViewController = controller; panel.behavior = .transient
    }

    private func apply(_ snapshot: QuotaSnapshot) {
        func line(_ title: String, _ window: QuotaWindow?) -> String {
            guard let window else { return "\(title)：不可用" }
            let percent = window.remaining.map { String(format: "%.0f%%", $0) } ?? "--"
            let reset = window.resetsAt.map { DateFormatter.localizedString(from: $0, dateStyle: .none, timeStyle: .short) } ?? "未知"
            return "\(title)：\(percent) · \(reset) 重置"
        }
        fiveLabel.stringValue = line("5 小时", snapshot.fiveHour)
        sevenLabel.stringValue = line("7 天", snapshot.sevenDay)
        updatedLabel.stringValue = "更新：" + DateFormatter.localizedString(from: snapshot.fetchedAt, dateStyle: .none, timeStyle: .medium)
        petView.apply(remaining: snapshot.fiveHour?.remaining)
        statusItem.button?.title = snapshot.fiveHour?.remaining.map { String(format: "🐣 %.0f%%", $0) } ?? "🐣 --"
    }

    @objc private func refresh() { client.refresh() }
    @objc private func togglePet() { petWindow.isVisible ? petWindow.orderOut(nil) : petWindow.orderFrontRegardless() }
    @objc private func togglePanel() {
        guard let button = statusItem.button else { return }
        if panel.isShown { panel.close() } else { panel.show(relativeTo: button.bounds, of: button, preferredEdge: .minY) }
    }
    @objc private func quit() { client.stop(); NSApp.terminate(nil) }
    func applicationWillTerminate(_ notification: Notification) { client.stop() }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
