# -*- coding: utf-8 -*-
"""生成 Stacklands 牌面背景 PNG：圆角矩形 + 黑色描边 + 分类色填充。

规格对应 Core/View/CardView.cs 的牌面比例（1.5 x 2 世界单位 = 3:4）。
输出到 UnityProject/Assets/AssetRaw/UIRaw/Atlas/Card/。
"""

import os

from PIL import Image, ImageDraw

# 最终尺寸（3:4），4 倍超采样抗锯齿
WIDTH, HEIGHT = 240, 320
SS = 4
CORNER_RADIUS = 26
BORDER_WIDTH = 10
BORDER_COLOR = (16, 16, 16, 255)

# 颜色与 Stacklands-Original卡牌配方与玩法逻辑.md 第 3 节的 14 种卡面颜色对应
CARDS = {
    "card_bg_pink": (242, 150, 146),          # 建筑与设施
    "card_bg_black": (58, 58, 58),            # 资源节点
    "card_bg_red": (205, 91, 81),             # 敌人与传送门
    "card_bg_gold": (227, 178, 60),           # 货币与宝物
    "card_bg_yellow": (246, 211, 107),        # 人口与职业
    "card_bg_silver": (199, 204, 212),        # 材料
    "card_bg_white": (250, 250, 250),         # 尸体
    "card_bg_green": (121, 180, 104),         # 种植资源（粪便）
    "card_bg_blue": (115, 139, 164),          # 点子与传闻
    "card_bg_orange": (244, 166, 104),        # 可直接食用
    "card_bg_light_orange": (249, 207, 157),  # 需加工的食材
    "card_bg_brown": (164, 112, 75),          # 友好动物
    "card_bg_purple": (168, 143, 197),        # 探索地点
    "card_bg_gray": (142, 147, 153),          # 装备与特殊结构
}

OUT_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "UnityProject", "Assets", "AssetRaw", "UIRaw", "Atlas", "Card",
)

# 卡包底图：尺寸对应 Core/View/BoosterView.cs 的卡包比例（1.6 x 2.2 世界单位 = 8:11）。
# 填充用近黑色而非纯黑，避免与描边糊成一团。
BOOSTER_WIDTH, BOOSTER_HEIGHT = 240, 330
BOOSTERS = {
    "booster_bg_black": (36, 36, 36),
}


def make_card(fill_rgb, width=WIDTH, height=HEIGHT):
    w, h = width * SS, height * SS
    radius = CORNER_RADIUS * SS
    border = BORDER_WIDTH * SS

    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # 黑色描边外框
    draw.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=BORDER_COLOR)
    # 彩色牌面
    draw.rounded_rectangle(
        [border, border, w - 1 - border, h - 1 - border],
        radius=max(radius - border, 1),
        fill=fill_rgb + (255,),
    )

    return image.resize((width, height), Image.LANCZOS)


def main():
    out_dir = os.path.normpath(OUT_DIR)
    os.makedirs(out_dir, exist_ok=True)
    for name, rgb in CARDS.items():
        path = os.path.join(out_dir, name + ".png")
        make_card(rgb).save(path)
        print("written:", path)
    for name, rgb in BOOSTERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_card(rgb, BOOSTER_WIDTH, BOOSTER_HEIGHT).save(path)
        print("written:", path)


if __name__ == "__main__":
    main()
