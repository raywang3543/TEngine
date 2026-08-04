# -*- coding: utf-8 -*-
"""生成 Stacklands 卡牌选中边框 PNG：圆角矩形环 + 透明内芯。

规格对应 Core/View/CardView.cs 的牌面比例（1.5 x 2 世界单位 = 3:4），
与 gen_card_backgrounds.py 的牌面底图同尺寸（240x320），叠在牌面正上方。
输出到 UnityProject/Assets/AssetRaw/UIRaw/Atlas/Card/。
"""

import os

from PIL import Image, ImageDraw

# 最终尺寸（3:4），4 倍超采样抗锯齿，与牌面底图一致
WIDTH, HEIGHT = 240, 320
SS = 4
CORNER_RADIUS = 26

# 颜色对应 CardView.RefreshOutline 原来的三态：默认黑、选中白、整堆拖动黄。
# 黑/白取与底图描边相同的 10px 宽度；黄色对应原来更大的描边缩放，取 16px。
BORDERS = {
    "card_border_black": ((16, 16, 16, 255), 10),
    "card_border_white": ((255, 255, 255, 255), 10),
    "card_border_yellow": ((255, 226, 92, 255), 16),
}

# 卡槽拖动反馈边框：尺寸与 gen_card_backgrounds.py 的 slot_bg_black 一致（280x310），
# 颜色对应卡牌配方的分类色（绿=可出售/是金币，红=不可出售/非金币）。
SLOT_WIDTH, SLOT_HEIGHT = 280, 310
SLOT_BORDERS = {
    "slot_border_green": ((121, 180, 104, 255), 12),
    "slot_border_red": ((205, 91, 81, 255), 12),
}

OUT_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "UnityProject", "Assets", "AssetRaw", "UIRaw", "Atlas", "Card",
)


def make_border(color, border_width, width=WIDTH, height=HEIGHT):
    w, h = width * SS, height * SS
    radius = CORNER_RADIUS * SS * width // WIDTH
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


def main():
    out_dir = os.path.normpath(OUT_DIR)
    os.makedirs(out_dir, exist_ok=True)
    for name, (color, border_width) in BORDERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_border(color, border_width).save(path)
        print("written:", path)
    for name, (color, border_width) in SLOT_BORDERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_border(color, border_width, SLOT_WIDTH, SLOT_HEIGHT).save(path)
        print("written:", path)


if __name__ == "__main__":
    main()
