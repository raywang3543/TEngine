# -*- coding: utf-8 -*-
"""生成 Stacklands 牌面背景 PNG：圆角矩形 + 黑色描边 + 分类色填充。

规格对应 Core/View/CardView.cs 的牌面比例（1.5 x 2 世界单位 = 3:4）。
输出到 UnityProject/Assets/AssetRaw/UIRaw/Atlas/Card/。
"""

import os

from PIL import Image, ImageDraw

# 最终尺寸（3:4），4 倍超采样抗锯齿
WIDTH, HEIGHT = 150, 200
SS = 4
CORNER_RADIUS = 26
BORDER_WIDTH = 5
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
BOOSTER_WIDTH, BOOSTER_HEIGHT = 160, 220
BOOSTERS = {
    "booster_bg_black": (36, 36, 36),
}

# 卡槽底图：尺寸 135x155（27:31，宽度较世界比例 1.4 x 1.55 略收窄）。
# 填充烘焙为出售槽/可购买商店槽的目标色 (10, 11, 10)，无独立描边，
# 其余状态由代码按目标色 / 底色的倍率染色还原。
# 圆角半径固定 30px（沿用 280 宽时的视觉弧度，不随新宽度等比缩小）。
SLOT_WIDTH, SLOT_HEIGHT = 135, 155
SLOT_CORNER_RADIUS = 30
SLOTS = {
    "slot_bg_black": (10, 11, 10),
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


def make_slot(fill_rgb, width=SLOT_WIDTH, height=SLOT_HEIGHT):
    """纯色圆角矩形，无描边，对应卡槽当前白底染色的表现。"""
    w, h = width * SS, height * SS
    radius = SLOT_CORNER_RADIUS * SS

    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=fill_rgb + (255,))

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
    for name, rgb in CARDS.items():
        path = os.path.join(out_dir, name + ".png")
        make_card(rgb).save(path)
        sync_meta_rect(path, WIDTH, HEIGHT)
        print("written:", path)
    for name, rgb in BOOSTERS.items():
        path = os.path.join(out_dir, name + ".png")
        make_card(rgb, BOOSTER_WIDTH, BOOSTER_HEIGHT).save(path)
        sync_meta_rect(path, BOOSTER_WIDTH, BOOSTER_HEIGHT)
        print("written:", path)
    for name, rgb in SLOTS.items():
        path = os.path.join(out_dir, name + ".png")
        make_slot(rgb).save(path)
        sync_meta_rect(path, SLOT_WIDTH, SLOT_HEIGHT)
        print("written:", path)


if __name__ == "__main__":
    main()
