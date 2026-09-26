# 宠物素材生成记录

方式：Codex 内置 imagegen。素材以用户提供的角色参考图和用户确认的站立草案为输入。用户已确认拥有公开再发布所需的权利，项目素材按仓库 MIT 许可证发布。原始参考照片未复制到本项目。

当前程序使用下面三张状态素材。生成方式：将 `source/pet-idle.png` 作为已确认的角色造型输入，将用户新提供的对应情绪照片作为姿势和表情输入，通过 Codex 内置 imagegen 重新绘制透明背景的全身图。三张图片均为 imagegen 输出，未经手工抠图。原始参考照片及其水印没有纳入程序包。

## pet-low.png

输入：已确认角色图 + 用户提供的低额度参考图（垂头、半闭眼、双臂垂下）。提示：保持相同的黄色梨形体态、浅色肚皮、绿色眼睛、深橄榄色手脚和 3D 黏土质感；表现明显低落与疲惫，完整站立身体和双脚可见；透明背景，无文字、水印、道具或新配饰。

## pet-mid.png

输入：已确认角色图 + 用户提供的中额度参考图（侧身、睁眼、双手扶肚子）。提示：保持同一角色身份、体型比例和材质；表现平静中性的状态，完整站立身体可见；透明背景，无文字、水印、道具或新配饰。

## pet-high.png

输入：已确认角色图 + 用户提供的高额度参考图（闭眼大笑、双手抱肚子）。提示：保持同一角色身份、体型比例和材质；表现开心大笑的状态，完整站立身体和双脚可见；透明背景，无文字、水印、道具或新配饰。

## 点击动作帧

方式：Codex 内置 imagegen，以对应状态的 PNG 为编辑目标，每张动作图都保持原角色比例与透明背景。当前程序通过动作图与 WPF 位移、倾斜、伸缩帧组合成短动画。

### pet-laugh.png

Use case: stylized-concept. Asset type: one click-animation keyframe for a Windows desktop pet. Input image is the exact approved HIGH-quota pet identity and standing pose. Edit the same full-body golden-yellow pear-shaped 3D clay creature into the peak of a belly-laugh: eyes closed in happy arcs, mouth opened even wider with teeth, both dark olive hands pressing and patting its large cream belly, shoulders slightly raised and torso leaning back a little. Preserve the same creature proportions, camera angle, yellow body, cream belly, dark hands and feet, soft clay shading and entire full-body framing. Transparent background with genuine alpha, no floor, no text, no watermark, no extra accessories. This must be a frame that can switch from the input image without looking like a different character.

### pet-glance.png

Use case: stylized-concept. Asset type: one click-animation keyframe for a Windows desktop pet. Input image is the exact approved MID-quota pet identity and neutral pose. Edit the same full-body golden-yellow pear-shaped 3D clay creature so it has turned its head and upper torso to look directly at the viewer for a brief curious glance, both green eyes now visibly facing forward, small neutral smile, its dark olive hands still resting on the cream belly. Preserve the same creature proportions, yellow body, cream belly, arms, legs, feet, clay shading and entire full-body framing. Transparent background with genuine alpha, no floor, no text, no watermark, no extra accessories. It must match the input as a nearby animation frame, not a new design.

### pet-cry.png

Use case: stylized-concept. Asset type: one click-animation keyframe for a Windows desktop pet. Input image is the exact approved LOW-quota pet identity and drooped full-body pose. Edit the same golden-yellow pear-shaped 3D clay creature at the peak of quiet crying: eyes squeezed partly shut, two visible blue tears down cheeks, lower lip trembling in a frown, one dark olive hand lifting to wipe a tear, shoulders slumped. Keep it gentle and cute, not frightening. Preserve the same silhouette, huge cream belly, yellow body, dark hands and feet, proportions, clay shading and full-body framing. Transparent background with genuine alpha, no floor, no text, no watermark, no extra accessories. It must match the input as a nearby animation frame.

## source/pet-idle.png

Use case: stylized-concept. Asset type: faithful character design draft for a Windows desktop pet. Input image 1 is the primary appearance reference. Input image 2 is a turnaround reference; ignore its phone screenshot frame and printed labels. Generate a single full-body neutral standing character on a truly transparent background, in the same overall design direction as the references: tall pear-shaped smooth golden-yellow 3D creature, small rounded head flowing into sloped shoulders and enormous round lower torso, pale cream oval belly, two large round green eyes with dark pupils, short small horizontal smile, thick yellow arms ending in dark olive-brown simple hands, long tapered legs with broad dark olive-brown three-toed feet. Friendly, slightly absurd and deadpan rather than conventionally baby-cute. Front three-quarter view, relaxed arms down at sides, entire character visible with clear margins, consistent proportions and soft studio clay-render shading. Be faithful to the reference creature's key visual features and silhouette; do not add ears, leaves, horns, fur, clothing, accessories, props or a new color palette. This is a newly rendered character design draft, not a copy-paste of the photo. No watermark, no text, no screenshot UI, no white backdrop, no floor; preserve true transparency for a desktop overlay asset.
