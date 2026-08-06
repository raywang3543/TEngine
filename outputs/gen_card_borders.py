# -*- coding: utf-8 -*-
"""生成 Stacklands 卡牌选中边框 PNG：圆角矩形环 + 透明内芯。

规格对应 Core/View/CardView.cs 的牌面比例（1.5 x 2 世界单位 = 3:4），
与 gen_card_backgrounds.py 的牌面底图同尺寸（240x320），叠在牌面正上方。
输出到 UnityProject/Assets/AssetRaw/UIRaw/Atlas/Card/。
"""

import os

from PIL import Image, ImageDraw

# 最终尺寸（3:4），4 倍超采样抗锯齿，与牌面底图一致
WIDTH, HEIGHT = 150, 200
SS = 4
CORNER_RADIUS = 26

# 颜色对应 CardView.RefreshOutline 原来的三态：默认黑、选中白、整堆拖动黄。
# 黑/白 5px；黄色对应整堆拖动的高亮，取 8px。
BORDERS = {
    "card_border_black": ((16, 16, 16, 255), 5),
    "card_border_white": ((255, 255, 255, 255), 5),
    "card_border_yellow": ((255, 226, 92, 255), 8),
}

# 卡槽拖动反馈边框：尺寸与 gen_card_backgrounds.py 的 slot_bg_black 一致（135x155），
# 颜色对应卡牌配方的分类色（绿=可出售/是金币，红=不可出售/非金币）。
# 圆角半径固定 30px（沿用 280 宽时的视觉弧度，不随新宽度等比缩小）。
SLOT_WIDTH, SLOT_HEIGHT = 135, 155
SLOT_CORNER_RADIUS = 30
SLOT_BORDERS = {
    "slot_border_green": ((121, 180, 104, 255), 6),
    "slot_border_red": ((205, 91, 81, 255), 6),
}

OUT_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "UnityProject", "Assets", "AssetRaw", "UIRaw", "Atlas", "Card",
)


def make_border(color, border_width, width=WIDTH, height=HEIGHT, corner_radius=CORNER_RADIUS):
    w, h = width * SS, height * SS
    radius = corner_radius * SS
    border = border_width * SS

    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # 外框填色
    draw.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=color)
    # 内芯挖空
    draw.rounded_rectangle(
        [border, border, w - 1 - border, h - 1 - border],
        radius=max(radius - border, 1),
        fill=(0, 0, 0, 0),
    )

    return image.resize((width, height), Image.LANCZOS)


def sync_meta_rect(png_path, width, height):
    """PNG 尺寸变化后同步 .meta 中 spriteSheet 的显式 rect。

    多精灵导入的贴图在 meta 里固化了子精灵矩形；只改 PNG 不改 rect 会让
    子精灵越界、YooAsset 取不到 Sprite（游戏内表现为回退纯色块）。
    """
    import re

    meta_path = png_path + ".meta"
    if not os.path.exists(meta_path):
        return
    with open(meta_path, encoding="utf-8") as f:
        content = f.read()
    pattern = r"(      rect:\n        serializedVersion: 2\n        x: 0\n        y: 0\n        width: )\d+(\n        height: )\d+"
    new_content = re.sub(pattern, lambda m: m.group(1) + str(width) + m.group(2) + str(height), content)
    if new_content != content:
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(new_content)
        print("meta rect synced:", meta_path)


def main():
    out_dir = os.path.normpath(OUT_DIR)
    os.makedirs(out_dir, exist_ok=True)
    for name, (color, border_width) in BORDERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_border(color, border_width).save(path)
        sync_meta_rect(path, WIDTH, HEIGHT)
        print("written:", path)
    for name, (color, border_width) in SLOT_BORDERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_border(color, border_width, SLOT_WIDTH, SLOT_HEIGHT, SLOT_CORNER_RADIUS).save(path)
        sync_meta_rect(path, SLOT_WIDTH, SLOT_HEIGHT)
        print("written:", path)


if __name__ == "__main__":
    main()
