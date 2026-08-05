# Stacklands（堆叠大陆）「Original」卡牌、配方与玩法逻辑

> 整理日期：2026-08-03
> 范围：Stacklands Wiki 的 `Cardopedia/Original`，即主大陆（Mainland）Original 卡池。
> 统计：121 张卡。按当前卡面颜色分为：粉 18、黑 6、红 12、金 4、黄 6、银 11、白 1、绿 1、蓝 34、橙 12、浅橙 3、棕 3、紫 6、灰 4。

## 1. 范围与版本说明

本文是玩法设计与实现参考，不是逐字翻译。英文名用于和游戏/Cardopedia 对照，中文名是便于阅读的译名。

- `Original` 指 Cardopedia 当前归入原版主大陆卡池的卡，不包括 The Island（岛屿）、Dark Forest（黑暗森林）、Order & Structure（秩序与结构）、Cursed Worlds（诅咒世界）和 Stacklands 2000（堆叠大陆 2000）的独立卡池。
- 本文采用 Wiki 当前页面和当前卡面的颜色，不是严格复原 2022 年 4 月 v1.0 的历史快照。现行版本把 0 食物点的 Egg（鸡蛋）、Potato（土豆）、Raw Meat（生肉）显示为浅橙色，并把 Poop（粪便）显示为绿色。
- Wiki 会持续按最新版游戏修订卡牌独立页面，因此个别页面会夹带其他地图或 DLC 的来源、用途和替代配方。本文只保留与 Original 主大陆闭环直接相关的部分。
- `Filter Crossroads（过滤十字路口）` 及其点子卡因资料跨版本、来源异常，均不纳入本文卡单。因此本文比 Cardopedia/Original 原始清单少 2 张。
- 配方中的卡牌上下顺序通常不重要；数量、参与卡和所需设施/村民才是匹配条件。点子卡是配方提示和收集项，不是配方原料。

## 2. 核心玩法循环

```text
开卡包/探索地点
    ↓
获得村民、资源节点、食物、敌人和点子
    ↓
村民采集 → 基础材料 → 加工材料 → 建筑/装备/料理
    ↓                         ↓
出售余卡赚金币            提升生产、人口和战力
    ↓                         ↓
购买更高级卡包 ← 完成任务解锁 ← 探索/建造/战斗
    ↓
建造神庙 + 献上金杯 → 击败 Demon（恶魔） → 主大陆主线完成
```

### 2.1 堆叠与计时

1. 玩家拖动卡牌，将能交互的卡叠在一起。
2. 合法组合会启动进度条；移动参与卡通常会中断或重置该次工作。
3. 采集和多数建造配方需要 Villager（村民）或其职业变体作为工人；料理、熔炼和设施加工通常由对应设施自动完成。
4. Tree（树）、Rock（岩石）、Berry Bush（浆果丛）等节点是有限资源；Lumber Camp（伐木场）、Quarry（采石场）、Iron Mine（铁矿）是需要工人持续工作的无限资源建筑。
5. 生产结果从堆叠中弹出。原料是否消耗取决于角色：材料和食材消耗，建筑/生产设施通常保留。

### 2.2 月份、生存和卡牌上限

- 一个 Moon 是主要时间周期：短 90 秒、普通 120 秒、长 200 秒。
- 月末按人口消耗食物。普通成人通常需要 2 点，Baby（婴儿）和 Dog（狗）各需要 1 点；食物不足时，未被喂饱的单位饿死并变为 Corpse（尸体）。
- Baby（婴儿）优先被喂食，Dog（狗）在 Baby（婴儿）之后、普通人类之前。扩张人口前必须预留稳定食物。
- 超出卡牌上限时，月末必须出售多余卡牌才能继续。Coin（金币）不计入卡牌上限；Shed（棚屋） `+4`、Warehouse（仓库） `+14`。
- 可暂停或使用 1×/5×速度。和平模式不生成 Strange Portal（奇怪传送门）/Rare Portal（稀有传送门）。

### 2.3 经济、任务和卡包

- 把可出售卡拖到出售区得到 Coin（金币）；Market（市场）用 60 秒按双倍价格出售。
- Coin（金币）用于购买主大陆卡包，也可给 Travelling Cart（旅行商车）换随机卡。卡包每个卡槽有独立掉落池和概率，不是从一个总池等概率抽取。
- 点子通常只掉落一次；已获得的点子会从相关卡槽移除，并由默认卡池（Berry Bush（浆果丛）、Rock（岩石）、Tree（树））补位。
- 普通卡包中的每张卡有 1% 概率成为闪卡；可出售闪卡价值为普通版本的 5 倍。
- Quest 是渐进式教学和内容门槛；完成一定数量的任务会解锁更高级卡包。

| 主大陆卡包 | 价格 | 张数 | 解锁条件 |
|---|---:|---:|---|
| A New World（新世界） | 免费 | 5 | 新存档自动获得，第一张固定为 Villager（村民） |
| Humble Beginnings（简陋开端） | 3 Coin（金币） | 3 | 完成 3 个任务 |
| Seeking Wisdom（寻求智慧） | 4 Coin（金币） | 4 | 完成 10 个任务 |
| Reap & Sow（收割与播种） | 10 Coin（金币） | 4 | 完成 16 个任务 |
| Curious Cuisine（好奇料理） | 10 Coin（金币） | 3 | 完成 22 个任务 |
| Logic and Reason（逻辑与理性） | 15 Coin（金币） | 4 | 完成 24 个任务 |
| The Armory（军械库） | 15 Coin（金币） | 3 | 完成 26 个任务 |
| Explorers（探险家卡包） | 20 Coin（金币） | 3 | 完成 30 个任务 |
| Order and Structure（秩序与结构） | 25 Coin（金币） | 4 | 完成 34 个任务 |
| New Weaponry（新式武器） | 免费 | 3 | 累计购买至少 10 个卡包后，首次清空一个卡包时获得 |

#### 2.3.1 卡槽与概率的读法

- 一个卡包按第 1、2、3……卡槽依次出卡，每个卡槽有自己的掉落池；下表中的概率只在同一格内比较。
- `点子池`中的 Idea/Rumor 是一次性掉落项。只要点子已通过任意方式发现，它就会从所有卡包的相应卡槽移除。
- Wiki 的卡包页面只明确列出了“该卡槽点子全部发现后”的常规卡概率；仍有点子时，不应把后备池概率直接当成最终抽取概率。
- 仅含已发现点子的卡槽会使用主大陆默认后备池：Berry Bush（浆果丛）、Rock（岩石）、Tree（树），各约 33%。
- 和平模式会把敌人卡槽替换为默认后备池。
- 下表的 `†` 表示卡包当前页面列出了该卡，但它不属于 `Cardopedia/Original` 的 123 张清单，是后续更新并入主大陆掉落池的内容。

#### 2.3.2 A New World（新世界）

新存档自动且仅获得一次，不可购买。五个卡槽都是固定结果，是开局最小生存闭环。

| 卡槽 | 固定卡牌 |
|---:|---|
| 1 | Villager（村民）（100%） |
| 2 | Berry Bush（浆果丛）（100%） |
| 3 | Rock（岩石）（100%） |
| 4 | Wood（木材）（100%） |
| 5 | Coin（金币）（100%） |

#### 2.3.3 Humble Beginnings（简陋开端）

价格 3 Coin（金币），完成 3 个任务解锁，共 3 张。三个卡槽使用相同的点子池和后备池。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1–3 | Growth（生长）、House（房屋）、Offspring（后代）、Stick（木棍） | Stone（石头） 19%；Wood（木材） 19%；Berry Bush（浆果丛） 13%；Rock（岩石） 13%；Soil（土壤） 13%；Tree（树） 13%；Key（钥匙） 3%；Rabbit（兔） 3%；Rat（老鼠） 3% |

补充规则：如果场上/库存中已经至少有一张 Soil（土壤），该包的 Soil（土壤）获取还受隐藏限制影响；想继续抽 Soil（土壤），可先把已有 Soil（土壤）建成 Garden（花园）/Farm（农场）。

#### 2.3.4 Seeking Wisdom（寻求智慧）

价格 4 Coin（金币），完成 10 个任务解锁，共 4 张。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | Brick（砖）、Campfire（篝火）、Growth（生长）、House（房屋）、Offspring（后代）、Plank（木板）、Shed（棚屋）、Spear（长矛）、Stick（木棍） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 2 | — | Stone（石头） 27%；Wood（木材） 27%；Flint（燧石） 13%；Poop（粪便） 13%；Stick（木棍） 13%；Coin（金币） 7% |
| 3 | Chicken（鸡）、Coin Chest（金币箱）、Garden（花园）、Lumber Camp（伐木场）、Quarry（采石场）、Shed（棚屋） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 4 | — | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |

#### 2.3.5 Reap & Sow（收割与播种）

价格 10 Coin（金币），完成 16 个任务解锁，共 4 张，主要提供食物、动物和种植能力。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | — | Apple（苹果） / Berry（浆果） / Carrot（胡萝卜） / Raw Meat（生肉），各 25% |
| 2 | — | Chicken（鸡） / Cow（牛） / Rabbit（兔） / Soil（土壤），各 25% |
| 3 | Chicken（鸡）、Coin Chest（金币箱）、Garden（花园）、Lumber Camp（伐木场）、Quarry（采石场）、Shed（棚屋） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 4 | — | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |

#### 2.3.6 Curious Cuisine（好奇料理）

价格 10 Coin（金币），完成 22 个任务解锁，共 3 张，集中解锁料理链。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | — | Egg（鸡蛋） / Milk（牛奶） / Mushroom（蘑菇） / Onion（洋葱） / Potato（土豆），各 20% |
| 2 | Cooked Meat（熟肉）、Frittata（烘蛋）、Fruit Salad（水果沙拉）、Milkshake（奶昔）、Omelette（煎蛋卷）、Stew（炖菜）、Stove（炉灶） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 3 | — | Apple（苹果） / Berry（浆果） / Carrot（胡萝卜） / Raw Meat（生肉），各 25% |

#### 2.3.7 Logic and Reason（逻辑与理性）

价格 15 Coin（金币），完成 24 个任务解锁，共 4 张，负责从基础资源过渡到工业设施。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | Chicken（鸡）、Coin Chest（金币箱）、Garden（花园）、Lumber Camp（伐木场）、Quarry（采石场）、Shed（棚屋） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 2 | — | Brick（砖） / Iron Ore（铁矿石） / Plank（木板），各 33% |
| 3 | Animal Pen（动物围栏）、Brickyard（砖厂）、Farm（农场）、Iron Bar（铁锭）、Iron Mine（铁矿）、Market（市场）、Sawmill（锯木厂）、Smelter（冶炼炉）、Sword（剑）、Temple（神庙）、Warehouse（仓库）；另含 Mess Hall（食堂）†、Smithy（铁匠铺）†、University（大学）† | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 4 | — | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |

`Smithy（铁匠铺）` 本身在 Original 结构清单中，但 `Idea: Smithy（点子：铁匠铺）` 不在 Cardopedia/Original 原始的 33 张点子清单中，因此这里仍按点子卡标记为 `†`。

#### 2.3.8 The Armory（军械库）

价格 15 Coin（金币），完成 26 个任务解锁，共 3 张。当前卡包页面已吸收大量后续主大陆装备内容。

| 卡槽 | 一次性点子/传闻池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | Original：Spear（长矛）、Sword（剑）、Rumor: Dark Forest（传闻：黑暗森林）；后续内容†：Axe（斧头）、Blunderbuss（火铳）、Bone Spear（骨矛）、Bone Staff（骨杖）、Boomerang（回旋镖）、Bow（弓）、Broken Bottle（碎瓶）、Club（棍棒）、Crossbow（弩）、Chainmail Armor（锁子甲）、Fishing Rod（钓鱼竿）、Forest Amulet（森林护符）、Golden Chestplate（金色胸甲）、Hammer（锤子）、Iron Shield（铁盾）、Magic Blade（魔法剑刃）、Magic Ring（魔法戒指）、Magic Staff（魔法杖）、Magic Tome（魔法典籍）、Magic Wand（魔法棒）、Mountain Amulet（山脉护符）、Pickaxe（镐）、Slingshot（弹弓）、Spiked Plank（钉刺木板）、Throwing Stars（手里剑）、Wizard Robe（巫师长袍）、Wooden Shield（木盾） | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 2 | — | Bear（熊）、Giant Rat（巨鼠）、Skeleton（骷髅）、Slime（史莱姆）、Wolf（狼），各 12.5%；Frog Man（蛙人）†、Goblin Shaman（哥布林萨满）†、Mimic（宝箱怪）†各 12.5% |
| 3 | — | Brick（砖） / Iron Ore（铁矿石） / Plank（木板），各 33% |

注意：卡槽 2 当前列有 8 个等概率敌人，总和 100%；只过滤 Original 敌人后概率不会重新归一化。

#### 2.3.9 Explorers（探险家）

价格 20 Coin（金币），完成 30 个任务解锁，共 3 张，没有一次性点子槽。

| 卡槽 | 常规池 |
|---:|---|
| 1 | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 2 | Bear（熊）、Giant Rat（巨鼠）、Skeleton（骷髅）、Slime（史莱姆）、Wolf（狼），各 12.5%；Frog Man（蛙人）†、Goblin Shaman（哥布林萨满）†、Mimic（宝箱怪）†各 12.5% |
| 3 | Forest（森林） / Mountain（山脉） / Old Village（古村落） / Plains（平原），各 25% |

#### 2.3.10 Order and Structure（秩序与结构）

价格 25 Coin（金币），完成 34 个任务解锁，共 4 张，是 Original 主大陆最高价卡包。

| 卡槽 | 一次性点子池 | 点子耗尽后的常规池 |
|---:|---|---|
| 1 | — | Iron Deposit（铁矿床）（100%） |
| 2 | Animal Pen（动物围栏）、Brickyard（砖厂）、Farm（农场）、Iron Bar（铁锭）、Iron Mine（铁矿）、Market（市场）、Sawmill（锯木厂）、Smelter（冶炼炉）、Sword（剑）、Temple（神庙）、Warehouse（仓库）；另含 Mess Hall（食堂）†、Smithy（铁匠铺）† | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 3 | 与卡槽 2 相同 | Berry Bush（浆果丛） / Rock（岩石） / Tree（树），各 33% |
| 4 | — | Brick（砖） / Iron Ore（铁矿石） / Plank（木板），各 33% |

#### 2.3.11 New Weaponry（新式武器）

累计购买至少 10 个卡包后，首次完整开完任一卡包时自动获得；每局仅一次，不可购买。

| 卡槽 | 固定规则 |
|---:|---|
| 1 | 随机 Equipment Idea（100%） |
| 2 | 随机 Equipment Idea（100%） |
| 3 | Rumor: Combat（传闻：战斗）（100%） |

#### 2.3.12 卡包选择逻辑总结

- 缺基础资源时购买 Humble Beginnings（简陋开端）/Seeking Wisdom（寻求智慧）；低价包也用于触发保底 Villager（村民）规则和寻找 Soil（土壤）。
- 食物不可持续时优先 Reap & Sow（收割与播种）；缺料理和高食物点配方时购买 Curious Cuisine（好奇料理）。
- Logic and Reason（逻辑与理性）是进入 Iron Bar（铁锭）、Farm（农场）、Smelter（冶炼炉）、Temple（神庙）等中后期链路的关键包。
- The Armory（军械库）提供战斗点子但可能直接生成敌人；没有基本战力时连续购买风险较高。
- Explorers（探险家卡包）稳定提供地点，但第二槽也会出敌人；地点探索会进一步产生资源和战斗。
- Order and Structure（秩序与结构）第一槽固定 Iron Deposit（铁矿床），适合稳定推进铁资源；两个点子槽加快补齐高级建筑点子。
- 点子接近收集完毕后，含点子槽的包会越来越像基础节点包；此时应按固定资源槽和地点槽选择，而不是继续为点子盲抽。

### 2.4 人口、职业与自动战斗

- 两名成人放入 House（房屋），20 秒得到 Baby（婴儿）；Baby（婴儿）放入 House（房屋），240 秒成长为 Villager（村民）。
- 给普通 Villager（村民）装备 Hand 卡会改变职业：Map（地图） → Explorer（探险家），Spear（长矛） → Militia（民兵），Sword（剑） → Swordsman（剑士）；卸下后恢复普通 Villager（村民）。
- 村民和敌对生物接触会进入自动战斗。玩家主动把村民拖向敌人发起的战斗允许撤出单位，适合保护残血角色。
- 攻击关系为 `Ranged > Melee > Magic > Ranged`；克制攻击伤害提高 40%。一次攻击依次判定命中、特殊效果、目标、伤害浮动、防御格挡、克制和暴击。
- 基础伤害有 50% 概率 `+1`；防御按档位格挡。即使完全格挡，仍有 50% 概率受到 1 点伤害。
- Combat Level 只是对生命、攻速、命中、伤害、防御和特殊效果的综合估值，本身不参与伤害计算。

### 2.5 威胁、探索与主线胜利

- 主大陆从 Moon 12 开始每 4 Moon 出现 Strange Portal（奇怪传送门）；每第 4 次应出现普通传送门时，改为强度约两倍的 Rare Portal（稀有传送门）。传送门在 30 秒后生成一组敌人，强度随 Moon 增长，Moon 46 后封顶。
- Travelling Cart（旅行商车）从 Moon 9 起在奇数 Moon 有 10% 概率出现，Moon 19 保底；每次支付 5 Coin（金币）得一张卡，第 6 次固定给 Golden Goblet（金杯）。
- Villager（村民）/Explorer（探险家）放在紫色地点上进行探索，地点按概率产出资源、敌人、宝箱或新地点。
- 终局链：建造 Temple（神庙） → 把 Golden Goblet（金杯）放到 Temple（神庙）并确认 → 生成 Demon（恶魔） → 击败 Demon（恶魔）。Demon（恶魔）是 Original 主大陆的最终 Boss。

## 3. 按卡牌颜色分类（121）

颜色首先表达卡牌的交互角色，但不能完全替代玩法类型。例如红色卡既有敌对生物，也有会生成敌群的传送门；蓝色卡同时包括配方点子与传闻。

| 颜色 | 数量 | 主要含义 |
|---|---:|---|
| Pink（粉色） | 18 | 玩家建造和使用的建筑/设施 |
| Black（黑色） | 6 | 可采集或可种植的资源节点 |
| Red（红色） | 12 | 敌对生物、Boss、敌袭传送门 |
| Gold（金色） | 4 | 货币、钥匙、重要宝物和宝箱 |
| Yellow（黄色） | 6 | 人口、工人和职业单位 |
| Silver（银色） | 11 | 基础或加工材料 |
| White（白色） | 1 | 死亡后的人类尸体 |
| Green（绿色） | 1 | 粪便/种植资源；不是 The Island（岛屿）的绿色鱼类卡 |
| Blue（蓝色） | 34 | 32 张配方点子和 2 张传闻 |
| Orange（橙色） | 12 | 可直接提供至少 1 点食物的食品 |
| Light Orange（浅橙色） | 3 | 当前不能直接供给食物、需要加工/孵化的食材 |
| Brown（棕色） | 3 | 友好动物，会周期生产资源 |
| Purple（紫色） | 6 | 可由村民探索的地点 |
| Gray（灰色） | 4 | 3 张装备，以及灰色特殊事件结构 Travelling Cart（旅行商车） |

### 3.1 Pink（粉色）：建筑与设施（18）

| # | 卡牌 | 功能定位 |
|---:|---|---|
| 1 | Animal Pen（动物围栏） | 动物收纳与限位 |
| 2 | Brickyard（砖厂） | Stone（石头）自动加工为 Brick（砖） |
| 3 | Campfire（篝火） | 基础烹饪设施 |
| 4 | Coin Chest（金币箱） | Coin（金币）收纳 |
| 5 | Farm（农场） | 高速种植设施 |
| 6 | Garden（花园） | 中速种植设施 |
| 7 | House（房屋） | 繁殖与 Baby（婴儿）成长 |
| 8 | Iron Mine（铁矿） | 无限 Iron Ore（铁矿石）来源 |
| 9 | Lumber Camp（伐木场） | 无限 Wood（木材）来源 |
| 10 | Market（市场） | 双倍价格出售卡牌 |
| 11 | Quarry（采石场） | 无限 Stone（石头）/Flint（燧石）来源 |
| 12 | Sawmill（锯木厂） | Wood（木材）自动加工为 Plank（木板） |
| 13 | Shed（棚屋） | 卡牌上限 +4 |
| 14 | Smelter（冶炼炉） | 熔炼 Iron Bar（铁锭） |
| 15 | Smithy（铁匠铺） | 高级装备制作工作站 |
| 16 | Stove（炉灶） | 高速烹饪设施 |
| 17 | Temple（神庙） | Golden Goblet（金杯）召唤 Demon（恶魔） |
| 18 | Warehouse（仓库） | 卡牌上限 +14 |

### 3.2 Black（黑色）：资源节点（6）

| # | 卡牌 | 节点产出/用途 |
|---:|---|---|
| 1 | Apple Tree（苹果树） | 采集 Apple（苹果）和 Stick（木棍），可由 Apple（苹果）种植 |
| 2 | Berry Bush（浆果丛） | 采集 Berry（浆果），可由 Berry（浆果）种植 |
| 3 | Iron Deposit（铁矿床） | 采集 Iron Ore（铁矿石）、Stone（石头），并可能产出 Coin（金币） |
| 4 | Rock（岩石） | 采集 Stone（石头）、Flint（燧石），并可能产出 Coin（金币） |
| 5 | Soil（土壤） | 最慢的食物种植基底 |
| 6 | Tree（树） | 采集 Wood（木材）、Stick（木棍）和 Apple（苹果），可由 Stick（木棍）种植 |

### 3.3 Red（红色）：敌人与传送门（12）

| # | 卡牌 | 类型/作用 |
|---:|---|---|
| 1 | Bear（熊） | 敌对生物 |
| 2 | Demon（恶魔） | Original 主大陆最终 Boss |
| 3 | Giant Rat（巨鼠） | 敌对生物 |
| 4 | Goblin（哥布林） | 敌对生物 |
| 5 | Rat（老鼠） | 敌对生物 |
| 6 | Skeleton（骷髅） | 敌对生物 |
| 7 | Slime（史莱姆） | 敌对生物；死亡分裂为 Small Slime（小史莱姆） |
| 8 | Small Slime（小史莱姆） | 敌对生物 |
| 9 | Wicked Witch（邪恶女巫） | Dark Forest（黑暗森林） Boss；Original 入口页仍将其列入红卡 |
| 10 | Wolf（狼） | 敌对生物；可用 Bone（骨头）驯化为 Dog（狗） |
| 11 | Strange Portal（奇怪传送门） | 周期生成普通强度敌群 |
| 12 | Rare Portal（稀有传送门） | 周期生成约双倍强度敌群 |

### 3.4 Gold（金色）：货币与宝物（4）

| # | 卡牌 | 作用 |
|---:|---|---|
| 1 | Coin（金币） | 主大陆货币，不计卡牌上限 |
| 2 | Golden Goblet（金杯） | 主线神器；放到 Temple（神庙）召唤 Demon（恶魔） |
| 3 | Key（钥匙） | 打开 Treasure Chest（宝箱） |
| 4 | Treasure Chest（宝箱） | 使用 Key（钥匙）打开并生成战利品 |

### 3.5 Yellow（黄色）：人口与职业（6）

| # | 卡牌 | 作用 |
|---:|---|---|
| 1 | Baby（婴儿） | 不能工作/战斗，在 House（房屋）中成长为 Villager（村民） |
| 2 | Dog（狗） | 可工作、战斗和装备，但工作速度较慢 |
| 3 | Explorer（探险家） | Map（地图）职业；探索更快、其他工作较慢 |
| 4 | Militia（民兵） | Spear（长矛）等民兵武器形成的战斗职业 |
| 5 | Swordsman（剑士） | Sword（剑）形成的战斗职业 |
| 6 | Villager（村民） | 基础成人、工人和战斗单位 |

### 3.6 Silver（银色）：材料（11）

| # | 卡牌 | 作用 |
|---:|---|---|
| 1 | Bone（骨头） | 驯化 Wolf（狼）、制作装备 |
| 2 | Brick（砖） | Stone（石头）加工材料，用于高级建筑 |
| 3 | Flint（燧石） | 点火、冶炼和矿业设施材料 |
| 4 | Iron Bar（铁锭） | Iron Ore（铁矿石）熔炼材料，用于高级建筑/装备 |
| 5 | Iron Ore（铁矿石） | 熔炼 Iron Bar（铁锭） |
| 6 | Magic Dust（魔法粉尘） | 魔法装备/后续高级设施材料 |
| 7 | Old Tome（古老典籍） | 研究未发现的 Idea |
| 8 | Plank（木板） | Wood（木材）加工材料，用于中高级建筑 |
| 9 | Stick（木棍） | Wood（木材）初级加工物，用于工具/装备/建筑 |
| 10 | Stone（石头） | 基础建材 |
| 11 | Wood（木材） | 基础建材、加工原料和熔炼燃料 |

### 3.7 White（白色）：尸体（1）

| # | 卡牌 | 作用 |
|---:|---|---|
| 1 | Corpse（尸体） | 人类死亡/挨饿后的结果；2 Corpse（尸体）可组成 Graveyard（墓地） |

### 3.8 Green（绿色）：种植资源（1）

| # | 卡牌 | 作用 |
|---:|---|---|
| 1 | Poop（粪便） | Rabbit（兔）等动物产出；可作为种植基底 |

### 3.9 Blue（蓝色）：点子与传闻（34）

| 子类 | 数量 | 卡牌 |
|---|---:|---|
| 基础/人口点子 | 5 | Idea: Coin Chest（点子：金币箱）、Growth（生长）、House（房屋）、Offspring（后代）、Stick（木棍） |
| 建筑/设施点子 | 15 | Idea: Animal Pen（点子：动物围栏）、Brickyard（砖厂）、Campfire（篝火）、Farm（农场）、Garden（花园）、Iron Mine（铁矿）、Lumber Camp（伐木场）、Market（市场）、Quarry（采石场）、Sawmill（锯木厂）、Shed（棚屋）、Smelter（冶炼炉）、Stove（炉灶）、Temple（神庙）、Warehouse（仓库） |
| 加工点子 | 3 | Idea: Brick（点子：砖）、Iron Bar（铁锭）、Plank（木板） |
| 料理/动物点子 | 7 | Idea: Chicken（点子：鸡）、Cooked Meat（熟肉）、Frittata（烘蛋）、Fruit Salad（水果沙拉）、Milkshake（奶昔）、Omelette（煎蛋卷）、Stew（炖菜） |
| 装备点子 | 2 | Idea: Spear（点子：长矛）、Sword（剑） |
| 传闻 | 2 | Rumor: Combat（传闻：战斗）、Rumor: Dark Forest（传闻：黑暗森林） |
| **合计** | **34** | 32 张 Idea + 2 张 Rumor |

### 3.10 Orange（橙色）：可直接食用（12）

| # | 卡牌 | 食物点 |
|---:|---|---:|
| 1 | Apple（苹果） | 2 |
| 2 | Berry（浆果） | 1 |
| 3 | Carrot（胡萝卜） | 2 |
| 4 | Cooked Meat（熟肉） | 2 |
| 5 | Frittata（烘蛋） | 3 |
| 6 | Fruit Salad（水果沙拉） | 3 |
| 7 | Milk（牛奶） | 1 |
| 8 | Milkshake（奶昔） | 2 |
| 9 | Mushroom（蘑菇） | 1 |
| 10 | Omelette（煎蛋卷） | 3 |
| 11 | Onion（洋葱） | 1 |
| 12 | Stew（炖菜） | 10 |

### 3.11 Light Orange（浅橙色）：不可直接供食（3）

| # | 卡牌 | 用途 |
|---:|---|---|
| 1 | Egg（鸡蛋） | 0 食物点；孵 Chicken（鸡）或制作蛋类料理 |
| 2 | Potato（土豆） | 0 食物点；Frittata（烘蛋）/Stew（炖菜）原料，也可种植 |
| 3 | Raw Meat（生肉） | 0 食物点；Cooked Meat（熟肉）/Stew（炖菜）原料 |

### 3.12 Brown（棕色）：友好动物（3）

| # | 卡牌 | 周期产出 |
|---:|---|---|
| 1 | Chicken（鸡） | 每约 90s 产 Egg（鸡蛋） |
| 2 | Cow（牛） | 每约 90s 产 Milk（牛奶） |
| 3 | Rabbit（兔） | 每约 90s 产 Poop（粪便） |

### 3.13 Purple（紫色）：探索地点（6）

| # | 卡牌 | 定位 |
|---:|---|---|
| 1 | Catacombs（地下墓穴） | 中高风险遗迹；Golden Goblet（金杯）固定来源 |
| 2 | Forest（森林） | 林木、浆果、动物与低阶敌人 |
| 3 | Graveyard（墓地） | 由 Corpse（尸体）合成；骨头、尸体、宝箱和敌人 |
| 4 | Mountain（山脉） | Iron Deposit（铁矿床）、Rock（岩石）、敌人与冶炼线索 |
| 5 | Old Village（古村落） | 人口、Old Tome（古老典籍）、加工资源和敌人 |
| 6 | Plains（平原） | 友好动物、基础食物、Soil（土壤）和敌人 |

### 3.14 Gray（灰色）：装备与特殊结构（4）

| # | 卡牌 | 装备效果 |
|---:|---|---|
| 1 | Map（地图） | Villager（村民） → Explorer（探险家）；探索速度提高 |
| 2 | Spear（长矛） | Villager（村民） → Militia（民兵）；伤害提高 |
| 3 | Sword（剑） | Villager（村民） → Swordsman（剑士）；攻速、命中和伤害提高 |
| 4 | Travelling Cart（旅行商车） | 灰色特殊结构；5 Coin（金币）换随机卡，第 6 次固定给 Golden Goblet（金杯） |

`Travelling Cart（旅行商车）` 同时被 Wiki 归入 Resource Nodes/Structures，但当前卡图和独立页面均为灰色，因此这里按卡面颜色归入灰色，而不是按“Resource Node 通常为黑色”的类别规则归类。

## 4. 按玩法类型的详细卡牌资料

以下保留按结构、村民、资源、点子、食物、生物、地点、传闻和装备组织的详细配方与数值，作为颜色索引的补充。

### 4.1 Structures（结构，28）

| # | 卡牌 | 获得/配方 | 核心作用 |
|---:|---|---|---|
| 1 | Animal Pen（动物围栏） | 2 Plank（木板） + 2 Wood（木材） + 1 Iron Bar（铁锭） + 1 Villager（村民），30s | 容纳最多 5 只动物并限制移动 |
| 2 | Apple Tree（苹果树） | Apple（苹果） + Poop（粪便）/Soil（土壤） 120s；+ Garden（花园） 90s；+ Farm（农场） 60s | 可采集 Apple（苹果）和 Stick（木棍）；有限节点 |
| 3 | Berry Bush（浆果丛） | Berry（浆果） + Poop（粪便）/Soil（土壤） 120s；+ Garden（花园） 90s；+ Farm（农场） 60s | 可采集 Berry（浆果）；有限节点 |
| 4 | Brickyard（砖厂） | 1 Brick（砖） + 1 Iron Bar（铁锭） + 1 Wood（木材） + 1 Villager（村民），30s | 2 Stone（石头） → 1 Brick（砖），10s |
| 5 | Campfire（篝火） | 1 Stick（木棍） + 1 Flint（燧石），30s | 基础烹饪设施；料理速度慢于 Stove（炉灶） |
| 6 | Coin Chest（金币箱） | 1 Coin（金币） + 2 Wood（木材），10s | 存放最多 100 Coin（金币） |
| 7 | Farm（农场） | 1 Soil（土壤） + 2 Brick（砖） + 2 Plank（木板） + 1 Villager（村民），40s | 复制可种植食物；快于 Garden（花园） |
| 8 | Garden（花园） | 1 Soil（土壤） + 2 Stone（石头） + 2 Wood（木材） + 1 Villager（村民），30s | 复制可种植食物；快于 Soil（土壤）、慢于 Farm（农场） |
| 9 | House（房屋） | 1 Stone（石头） + 2 Wood（木材） + 1 Villager（村民），30s | 2 成人 → Baby（婴儿）（20s）；Baby（婴儿） → Villager（村民）（240s） |
| 10 | Iron Deposit（铁矿床） | Mountain（山脉）探索或卡包 | Villager（村民）采出 Iron Ore（铁矿石）、Stone（石头），并可能产出 Coin（金币）；有限节点 |
| 11 | Iron Mine（铁矿） | 2 Flint（燧石） + 1 Wood（木材） + 1 Stone（石头） + 1 Villager（村民），30s | 工人持续产出 Iron Ore（铁矿石），兼有 Stone（石头）/Coin（金币） |
| 12 | Lumber Camp（伐木场） | 3 Wood（木材） + 1 Stone（石头） + 1 Villager（村民），30s | 工人每约 15s 产 1 Wood（木材）；无限来源 |
| 13 | Market（市场） | 3 Coin（金币） + 1 Plank（木板） + 1 Brick（砖） + 1 Villager（村民），30s | 60s 后以双倍基础售价卖出一张卡 |
| 14 | Quarry（采石场） | 3 Stone（石头） + 1 Wood（木材） + 1 Villager（村民），30s | 工人持续产出 Stone（石头）/Flint（燧石）；无限来源 |
| 15 | Rare Portal（稀有传送门） | 周期事件 | 30s 后生成敌群，威胁阈值约为同 Moon 普通传送门的两倍 |
| 16 | Rock（岩石） | 卡包、Mountain（山脉） | Villager（村民）采出 Stone（石头）、Flint（燧石），并可能产出 Coin（金币）；有限节点 |
| 17 | Sawmill（锯木厂） | 1 Plank（木板） + 1 Stone（石头） + 1 Iron Bar（铁锭） + 1 Villager（村民），30s | 2 Wood（木材） → 1 Plank（木板），10s |
| 18 | Shed（棚屋） | 1 Stone（石头） + 1 Wood（木材） + 1 Stick（木棍） + 1 Villager（村民），30s | 卡牌上限 +4 |
| 19 | Smelter（冶炼炉） | 2 Flint（燧石） + 2 Brick（砖） + 1 Plank（木板） + 1 Villager（村民），30s | Wood（木材） + Iron Ore（铁矿石） → Iron Bar（铁锭），10s |
| 20 | Smithy（铁匠铺） | 2 Iron Bar（铁锭） + 2 Brick（砖） + 1 Villager（村民），30s | 制作更高级装备的工作站；Original 清单中无对应点子卡 |
| 21 | Soil（土壤） | 卡包、Plains（平原）/Graveyard（墓地）、商车 | 最慢的种植基底；也用于 Garden（花园）/Farm（农场） |
| 22 | Stove（炉灶） | 1 Flint（燧石） + 1 Brick（砖） + 1 Iron Bar（铁锭） + 1 Villager（村民），30s | 和 Campfire（篝火）使用相同料理配方，耗时约为其 30% |
| 23 | Strange Portal（奇怪传送门） | Moon 周期事件 | 30s 后生成随进度增强的敌群 |
| 24 | Temple（神庙） | 5 Plank（木板） + 5 Brick（砖） + 3 Iron Bar（铁锭） + 3 Villager（村民），180s | 放入 Golden Goblet（金杯）后召唤 Demon（恶魔） |
| 25 | Travelling Cart（旅行商车） | Moon 随机事件 | 5 Coin（金币）换随机卡；第 6 次固定 Golden Goblet（金杯） |
| 26 | Treasure Chest（宝箱） | 探索、商车 | Key（钥匙） + Treasure Chest（宝箱）打开，产生食物、资源、装备等战利品 |
| 27 | Tree（树） | 卡包、Forest（森林）；或 Stick（木棍） + Poop（粪便）/Soil（土壤）/Garden（花园）/Farm（农场） | Villager（村民）采出 Wood（木材）、Stick（木棍）和 Apple（苹果）；有限节点 |
| 28 | Warehouse（仓库） | 1 Iron Bar（铁锭） + 1 Stone（石头） + 1 Villager（村民），30s | 卡牌上限 +14 |

### 4.2 Villagers（村民，6）

| # | 卡牌 | 食物/月 | 获得/转换 | 逻辑 |
|---:|---|---:|---|---|
| 1 | Baby（婴儿） | 1 | House（房屋） + 2 成人，20s | 不能工作、战斗或装备；House（房屋）中 240s 长成 Villager（村民） |
| 2 | Dog（狗） | 1 | Wolf（狼） + Bone（骨头）；2 Dog（狗） + House（房屋）可繁殖 Dog（狗） | 能工作、战斗和装备，但所有工作约慢一倍；死亡/挨饿变 Corpse（尸体） |
| 3 | Explorer（探险家） | 2 | Villager（村民）装备 Map（地图） | 探索地点快一倍，其他工作慢 20%；卸 Map（地图）恢复 Villager（村民） |
| 4 | Militia（民兵） | 2 | Villager（村民）装备 Spear（长矛）等民兵武器 | 近战职业表现由装备决定；卸下武器恢复 Villager（村民） |
| 5 | Swordsman（剑士） | 2 | Villager（村民）装备 Sword（剑） | 强化战斗职业；卸 Sword（剑）恢复 Villager（村民） |
| 6 | Villager（村民） | 2 | 初始卡包、Old Village（古村落）、Baby（婴儿） + House（房屋） 240s | 基础工人和战斗单位；15 HP，近战，死亡/挨饿变 Corpse（尸体） |

### 4.3 Resources（资源，16）

| # | 卡牌 | 主要来源/配方 | 主要用途 |
|---:|---|---|---|
| 1 | Bone（骨头） | 击杀动物/敌人、Graveyard（墓地）、宝箱、商车 | Wolf（狼） → Dog（狗）；部分装备材料 |
| 2 | Brick（砖） | 3 Stone（石头） + Villager（村民），30s；或 Brickyard（砖厂）中 2 Stone（石头），10s | 高级建筑、Temple（神庙） |
| 3 | Coin（金币） | 出售卡、宝箱、Rock（岩石）、Iron Deposit（铁矿床）、Iron Mine（铁矿）及敌人掉落 | 购买卡包、商车；不计卡牌上限且不能出售 |
| 4 | Corpse（尸体） | 村民死亡/挨饿、地点、宝箱 | 2 Corpse（尸体） → Graveyard（墓地） |
| 5 | Flint（燧石） | Rock（岩石）、Quarry（采石场）、卡包、宝箱 | Campfire（篝火）、Smelter（冶炼炉）、Stove（炉灶）、Iron Mine（铁矿）等 |
| 6 | Golden Goblet（金杯） | 商车第 6 次购买；Catacombs（地下墓穴）第 4 次探索 | 放到 Temple（神庙）召唤 Demon（恶魔）；不可出售 |
| 7 | Iron Bar（铁锭） | Smelter（冶炼炉） + 1 Iron Ore（铁矿石） + 1 Wood（木材），10s | 高级建筑、Sword（剑）；也可由地点/商车/宝箱获得 |
| 8 | Iron Ore（铁矿石） | Iron Deposit（铁矿床）、Iron Mine（铁矿）、卡包、宝箱 | 熔炼 Iron Bar（铁锭） |
| 9 | Key（钥匙） | 卡包、敌人、商车、宝箱 | 打开 Treasure Chest（宝箱） |
| 10 | Magic Dust（魔法粉尘） | Skeleton（骷髅）/Small Slime（小史莱姆）等敌人、宝箱 | 原版清单中主要是后续高级装备/设施材料；可出售 |
| 11 | Old Tome（古老典籍） | 商车、宝箱、Old Village（古村落） | Villager（村民）研究后获得尚未发现的 Idea；点子全收集后可给 Map（地图） |
| 12 | Plank（木板） | 3 Wood（木材） + Villager（村民），30s；或 Sawmill（锯木厂）中 2 Wood（木材），10s | 中高级建筑和 Temple（神庙） |
| 13 | Poop（粪便） | Rabbit（兔）周期产出、动物/敌人、宝箱 | 作为种植基底 |
| 14 | Stick（木棍） | 1 Wood（木材） + Villager（村民），10s；Tree（树）/宝箱 | Campfire（篝火）、Shed（棚屋）、Spear（长矛）、Sword（剑） |
| 15 | Stone（石头） | Rock（岩石）、Quarry（采石场）、矿床/矿井、卡包、宝箱 | Brick（砖）和大量建筑 |
| 16 | Wood（木材） | Tree（树）、Lumber Camp（伐木场）、卡包、地点、宝箱 | Stick（木棍）、Plank（木板）、基础建筑、熔炼燃料 |

### 4.4 Ideas（点子，32）

点子卡本身通常售价 1 Coin（金币）。获得点子意味着 Cardopedia 显示配方，但真正的玩法对象是右侧“结果卡”。

| # | 点子卡 | 配方 → 结果 |
|---:|---|---|
| 1 | Idea: Animal Pen（点子：动物围栏） | 2 Plank（木板） + 2 Wood（木材） + 1 Iron Bar（铁锭） + 1 Villager（村民） → Animal Pen（动物围栏） |
| 2 | Idea: Brick（点子：砖） | 3 Stone（石头） + 1 Villager（村民） → Brick（砖） |
| 3 | Idea: Brickyard（点子：砖厂） | 1 Brick（砖） + 1 Iron Bar（铁锭） + 1 Wood（木材） + 1 Villager（村民） → Brickyard（砖厂） |
| 4 | Idea: Campfire（点子：篝火） | 1 Stick（木棍） + 1 Flint（燧石） → Campfire（篝火） |
| 5 | Idea: Chicken（点子：鸡） | 1 Chicken（鸡） + 1 Egg（鸡蛋） → Chicken（鸡） |
| 6 | Idea: Coin Chest（点子：金币箱） | 1 Coin（金币） + 2 Wood（木材） → Coin Chest（金币箱） |
| 7 | Idea: Cooked Meat（点子：熟肉） | 1 Campfire（篝火） + 1 Raw Meat（生肉） → Cooked Meat（熟肉） |
| 8 | Idea: Farm（点子：农场） | 1 Soil（土壤） + 2 Brick（砖） + 2 Plank（木板） + 1 Villager（村民） → Farm（农场） |
| 9 | Idea: Frittata（点子：烘蛋） | 1 Campfire（篝火） + 1 Egg（鸡蛋） + 1 Potato（土豆） → Frittata（烘蛋） |
| 10 | Idea: Fruit Salad（点子：水果沙拉） | 1 Apple（苹果） + 1 Berry（浆果） → Fruit Salad（水果沙拉） |
| 11 | Idea: Garden（点子：花园） | 1 Soil（土壤） + 2 Stone（石头） + 2 Wood（木材） + 1 Villager（村民） → Garden（花园） |
| 12 | Idea: Growth（点子：生长） | 1 Berry（浆果） + 1 Soil（土壤） → Berry Bush（浆果丛） |
| 13 | Idea: House（点子：房屋） | 2 Wood（木材） + 1 Stone（石头） + 1 Villager（村民） → House（房屋） |
| 14 | Idea: Iron Bar（点子：铁锭） | 1 Smelter（冶炼炉） + 1 Wood（木材） + 1 Iron Ore（铁矿石） → Iron Bar（铁锭） |
| 15 | Idea: Iron Mine（点子：铁矿） | 2 Flint（燧石） + 1 Wood（木材） + 1 Stone（石头） + 1 Villager（村民） → Iron Mine（铁矿） |
| 16 | Idea: Lumber Camp（点子：伐木场） | 3 Wood（木材） + 1 Stone（石头） + 1 Villager（村民） → Lumber Camp（伐木场） |
| 17 | Idea: Market（点子：市场） | 1 Brick（砖） + 1 Plank（木板） + 3 Coin（金币） + 1 Villager（村民） → Market（市场） |
| 18 | Idea: Milkshake（点子：奶昔） | 1 Milk（牛奶） + 1 Berry（浆果） → Milkshake（奶昔） |
| 19 | Idea: Offspring（点子：后代） | 1 House（房屋） + 2 Villager（村民） → Baby（婴儿） |
| 20 | Idea: Omelette（点子：煎蛋卷） | 1 Campfire（篝火） + 2 Egg（鸡蛋） → Omelette（煎蛋卷） |
| 21 | Idea: Plank（点子：木板） | 3 Wood（木材） + 1 Villager（村民） → Plank（木板） |
| 22 | Idea: Quarry（点子：采石场） | 3 Stone（石头） + 1 Wood（木材） + 1 Villager（村民） → Quarry（采石场） |
| 23 | Idea: Sawmill（点子：锯木厂） | 1 Plank（木板） + 1 Stone（石头） + 1 Iron Bar（铁锭） + 1 Villager（村民） → Sawmill（锯木厂） |
| 24 | Idea: Shed（点子：棚屋） | 1 Wood（木材） + 1 Stone（石头） + 1 Stick（木棍） + 1 Villager（村民） → Shed（棚屋） |
| 25 | Idea: Smelter（点子：冶炼炉） | 2 Flint（燧石） + 2 Brick（砖） + 1 Plank（木板） + 1 Villager（村民） → Smelter（冶炼炉） |
| 26 | Idea: Spear（点子：长矛） | 1 Wood（木材） + 2 Stick（木棍） → Spear（长矛） |
| 27 | Idea: Stew（点子：炖菜） | 1 Campfire（篝火） + 1 Potato（土豆） + 1 Raw Meat（生肉） + 1 Onion（洋葱） + 1 Carrot（胡萝卜） → Stew（炖菜） |
| 28 | Idea: Stick（点子：木棍） | 1 Wood（木材） + 1 Villager（村民） → Stick（木棍） |
| 29 | Idea: Stove（点子：炉灶） | 1 Brick（砖） + 1 Iron Bar（铁锭） + 1 Flint（燧石） + 1 Villager（村民） → Stove（炉灶） |
| 30 | Idea: Sword（点子：剑） | 1 Iron Bar（铁锭） + 2 Stick（木棍） → Sword（剑） |
| 31 | Idea: Temple（点子：神庙） | 5 Plank（木板） + 5 Brick（砖） + 3 Iron Bar（铁锭） + 3 Villager（村民） → Temple（神庙） |
| 32 | Idea: Warehouse（点子：仓库） | 1 Stone（石头） + 1 Iron Bar（铁锭） + 1 Villager（村民） → Warehouse（仓库） |

### 4.5 Food（食物，15）

| # | 卡牌 | 食物点 | 售价 | 来源/配方与作用 |
|---:|---|---:|---:|---|
| 1 | Apple（苹果） | 2 | 2 | Apple Tree（苹果树）/Tree（树）、卡包、Forest（森林）；可种 Apple Tree（苹果树）、做 Fruit Salad（水果沙拉） |
| 2 | Berry（浆果） | 1 | 1 | Berry Bush（浆果丛）、卡包、宝箱；可种 Berry Bush（浆果丛）、做 Fruit Salad（水果沙拉）/Milkshake（奶昔） |
| 3 | Carrot（胡萝卜） | 2 | 2 | 可在 Poop（粪便）/Soil（土壤）/Garden（花园）/Farm（农场）自我复制；Stew（炖菜）原料 |
| 4 | Cooked Meat（熟肉） | 2 | 3 | Raw Meat（生肉） + Campfire（篝火） 60s，或 + Stove（炉灶） 18s |
| 5 | Egg（鸡蛋） | 0 | 1 | Chicken（鸡）每 90s 生产；不能生吃；繁殖 Chicken（鸡）、做蛋类料理 |
| 6 | Frittata（烘蛋） | 3 | 3 | Egg（鸡蛋） + Potato（土豆） + Campfire（篝火） 60s，或 + Stove（炉灶） 18s；完整食用可获得一 Moon 的 Well Fed（工作速度 +100%） |
| 7 | Fruit Salad（水果沙拉） | 3 | 5 | Apple（苹果） + Berry（浆果），10s |
| 8 | Milk（牛奶） | 1 | 1 | Cow（牛）每 90s 生产、地点/卡包；做 Milkshake（奶昔） |
| 9 | Milkshake（奶昔） | 2 | 5 | Milk（牛奶） + Berry（浆果），约 10s |
| 10 | Mushroom（蘑菇） | 1 | 2 | Forest（森林）/Plains（平原）/卡包；可在种植基底自我复制 |
| 11 | Omelette（煎蛋卷） | 3 | 5 | 2 Egg（鸡蛋） + Campfire（篝火） 90s，或 + Stove（炉灶） 27s |
| 12 | Onion（洋葱） | 1 | 2 | Plains（平原）/卡包；可种植；Stew（炖菜）原料 |
| 13 | Potato（土豆） | 0 | 2 | 卡包；可种植；不能生吃，Frittata（烘蛋）/Stew（炖菜）原料 |
| 14 | Raw Meat（生肉） | 0 | 3 | 击杀动物/敌人、卡包、宝箱；Cooked Meat（熟肉）/Stew（炖菜）原料 |
| 15 | Stew（炖菜） | 10 | 10 | Raw Meat（生肉） + Carrot（胡萝卜） + Onion（洋葱） + Potato（土豆） + Campfire（篝火） 120s，或 + Stove（炉灶） 36s；完整食用可获得一 Moon 的 Well Fed |

### 4.6 Mobs（生物，13）

`友好`表示不会主动触发敌对战斗，但仍有战斗属性；攻击速度/命中等会受当前版本平衡调整，表中保留 Wiki 当前值。

| # | 卡牌 | 阵营 | 等级 / HP | 战斗与产出逻辑 |
|---:|---|---|---|---|
| 1 | Bear（熊） | 敌对 | 22 / 25 | 近战；10% 暴击；掉 Raw Meat（生肉）、Poop（粪便）、Bone（骨头），可能掉 Bear Claw（熊爪） |
| 2 | Chicken（鸡） | 友好 | 6 / 5 | 每 90s 产 Egg（鸡蛋）；Chicken（鸡） + Egg（鸡蛋）可孵出新 Chicken（鸡） |
| 3 | Cow（牛） | 友好 | 9 / 5 | 每 90s 产 Milk（牛奶）；攻击有 10% 概率眩晕 5s |
| 4 | Demon（恶魔） | Boss | 174 / 299 | 快速近战；5% 概率眩晕全部敌方 5s；Temple（神庙） + Goblet 召唤 |
| 5 | Giant Rat（巨鼠） | 敌对 | 19 / 20 | 普通速度近战；掉 Raw Meat（生肉）/Bone（骨头），可能掉 Rat Crown（鼠王冠） |
| 6 | Goblin（哥布林） | 敌对 | 8 / 10 | 慢速近战；可掉 Magic Dust（魔法粉尘）、Map（地图）或 Goblin Hat（哥布林帽） |
| 7 | Rabbit（兔） | 友好 | 8 / 5 | 每 90s 产 Poop（粪便）；攻击有 10% 概率给自身 Frenzy；可掉肉/胡萝卜等 |
| 8 | Rat（老鼠） | 敌对 | 6 / 5 | 普通速度近战；主要掉 Raw Meat（生肉）或 Coin（金币） |
| 9 | Skeleton（骷髅） | 敌对 | 12 / 12 | 慢速近战；5% 暴击；可掉 Bone（骨头）、Magic Dust（魔法粉尘）或装备 |
| 10 | Slime（史莱姆） | 敌对 | 14 / 15 | 死亡后分裂为 3 个 Small Slime（小史莱姆） |
| 11 | Small Slime（小史莱姆） | 敌对 | 4 / 4 | 很慢的近战；可掉 Coin（金币）、Egg（鸡蛋）、Magic Dust（魔法粉尘）、Key（钥匙） |
| 12 | Wicked Witch（邪恶女巫） | Boss/敌对 | 160 / 300 | 魔法攻击；来自 Dark Forest（黑暗森林）第 9 波且每局仅一次。虽列于 Original 页，实际逻辑跨到 Dark Forest（黑暗森林） |
| 13 | Wolf（狼） | 敌对 | 18 / 20 | 普通速度近战；Wolf（狼） + Bone（骨头） → Dog（狗）；可掉 Raw Meat（生肉）/Wolf Head（狼头） |

### 4.7 Locations（地点，6）

地点通常由 Villager（村民）探索 60s；Explorer（探险家）约快一倍。概率会随版本和扩展内容调整，以下聚焦 Original 闭环中的关键结果。

| # | 地点 | 获得方式 | Original 主要探索结果/规则 |
|---:|---|---|---|
| 1 | Catacombs（地下墓穴） | Forest（森林）/Graveyard（墓地）/Mountain（山脉）/Old Village（古村落） | 敌人或 Treasure Chest（宝箱）；第 4 次固定 Golden Goblet（金杯），第 5 次探索后消失；场上通常仅允许一个 |
| 2 | Forest（森林） | Explorers（探险家卡包）卡包 | Rabbit（兔）、Goblin（哥布林）/Rat（老鼠）/Slime（史莱姆）、Mushroom（蘑菇）/Apple（苹果）、Tree（树）/Berry Bush（浆果丛）/Apple Tree（苹果树）、Stick（木棍）、Catacombs（地下墓穴）、Treasure Chest（宝箱） |
| 3 | Graveyard（墓地） | 2 Corpse（尸体），15s | Skeleton（骷髅）、Soil（土壤）、Bone（骨头）、Treasure Chest（宝箱）、Catacombs（地下墓穴）、Corpse（尸体） |
| 4 | Mountain（山脉） | Explorers（探险家卡包）卡包 | Goblin（哥布林）/Rat（老鼠）/Slime（史莱姆）、Iron Deposit（铁矿床）、Rock（岩石）、Idea: Smelter（点子：冶炼炉）、Treasure Chest（宝箱）、Catacombs（地下墓穴） |
| 5 | Old Village（古村落） | Explorers（探险家卡包）卡包 | Slime（史莱姆）/Goblin（哥布林）/Rat（老鼠）、Milk（牛奶）、Wood（木材）、Iron Bar（铁锭）、Coin（金币）、Villager（村民）、Corpse（尸体）、Treasure Chest（宝箱）、Catacombs（地下墓穴）、Old Tome（古老典籍） |
| 6 | Plains（平原） | Explorers（探险家卡包）卡包 | Chicken（鸡）、Cow（牛）、Rat（老鼠）、Wolf（狼）、Onion（洋葱）/Mushroom（蘑菇）/Carrot（胡萝卜）/Milk（牛奶）、Soil（土壤） |

### 4.8 Rumors（传闻，2）

| # | 卡牌 | 信息/作用 |
|---:|---|---|
| 1 | Rumor: Combat（传闻：战斗） | 提示克制环：Melee > Magic > Ranged > Melee；克制伤害 +40% |
| 2 | Rumor: Dark Forest（传闻：黑暗森林） | 提示 Strange Portal（奇怪传送门）背后存在 Dark Forest（黑暗森林）；属于跨地图引导信息 |

### 4.9 Equipment（装备，3）

| # | 卡牌 | 获得/配方 | 装备效果 |
|---:|---|---|---|
| 1 | Map（地图） | Goblin（哥布林）、Old Tome（古老典籍）点子全收集后的产物、商车、宝箱 | Hand 装备；Villager（村民） → Explorer（探险家）；伤害档位 -1，但探索速度 ×2 |
| 2 | Spear（长矛） | 1 Wood（木材） + 2 Stick（木棍），20s；商车/宝箱 | Hand/近战；伤害档位 +2；Villager（村民） → Militia（民兵） |
| 3 | Sword（剑） | 1 Iron Bar（铁锭） + 2 Stick（木棍），15s；宝箱等 | Hand/近战；攻速、命中、伤害各 +1 档；Villager（村民） → Swordsman（剑士） |

## 5. 基础版任务（56 项）

任务完成记录可跨局保留，并按累计完成数量解锁主大陆卡包。Wiki 的 Quests（任务）页面把基础版、免费内容更新和 DLC 任务放在同一张表中；本节只保留不依赖 DLC 或扩展地图卡牌的 56 项任务：19 项 Main Quests（主线任务）和 37 项 Side Quests（支线任务）。

明确排除：The Grand Scheme（宏伟计划）中从 Build a Rowboat（建造划艇）开始的岛屿任务，以及 Marooned（流落荒岛）、Mystery of the Island（岛屿之谜）、The Dark Forest（黑暗森林）、Meet the Shaman（会见萨满）、Island Grub（岛屿美食）、Island Ambitions（岛屿抱负）、Strengthen Up（强化）、三个 Cursed Worlds（诅咒世界）和 Stacklands 2000（堆叠大陆 2000）的全部任务。

### 5.1 Main Quests（主线任务，19 项）

#### Welcome（欢迎，12 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Open the Booster Pack（打开卡包） | 点击并开完初始 A New World（新世界）卡包。 |
| 2 | Drag the Villager on top of the Berry Bush（把村民拖到浆果丛上） | 将 Villager（村民）堆到 Berry Bush（浆果丛）上。 |
| 3 | Mine a Rock using a Villager（让村民开采岩石） | 将 Villager（村民）堆到 Rock（岩石）上。 |
| 4 | Sell a Card（出售一张卡） | 把任意可出售卡拖到顶部出售槽。 |
| 5 | Buy the Humble Beginnings Pack（购买简陋开端卡包） | 将 3 Coin（金币）堆到 Humble Beginnings（简陋开端）卡包上。 |
| 6 | Harvest a Tree using a Villager（让村民采集树木） | 将 Villager（村民）堆到 Tree（树）上。 |
| 7 | Make a Stick from Wood（用木材制作木棍） | Wood（木材） + Villager（村民） → Stick（木棍）。 |
| 8 | Pause using the play icon in the top right corner（用右上角播放图标暂停） | 点击右上角速度/播放按钮切换到暂停。 |
| 9 | Grow a Berry Bush using Soil（用土壤种植浆果丛） | Berry（浆果）堆到 Soil（土壤）上；Wiki 也列出 Garden（花园）或 Farm（农场）可触发。 |
| 10 | Build a House（建造房屋） | 2 Wood（木材） + 1 Stone（石头） + 1 Villager（村民） → House（房屋）。 |
| 11 | Get a Second Villager（获得第二名村民） | 从卡包获得另一张 Villager（村民）。 |
| 12 | Create Offspring（繁育后代） | House（房屋） + 2 Villager（村民） → Baby（婴儿）。 |

#### The Grand Scheme（宏伟计划，基础版部分 7 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Unlock All Packs（解锁全部卡包） | 累计完成足够任务，解锁所有基础主大陆卡包；当前卡包表的最高任务门槛为 34 项。 |
| 2 | Get 3 Villagers（拥有三名村民） | 场上同时拥有 3 张成人 Villager（村民）；Baby（婴儿）须先成长。 |
| 3 | Find the Catacombs（找到地下墓穴） | 探索 Old Village（古村落）、Mountain（山脉）、Graveyard（墓地）或 Forest（森林）获得 Catacombs（地下墓穴）。 |
| 4 | Find a Mysterious Artifact（找到神秘神器） | 从 Travelling Cart（旅行商车）或 Catacombs（地下墓穴）获得 Golden Goblet（金杯）。 |
| 5 | Build a Temple（建造神庙） | 5 Plank（木板） + 5 Brick（砖） + 3 Iron Bar（铁锭） + 3 Villager（村民） → Temple（神庙）。 |
| 6 | Bring the Goblet to the Temple（把金杯带到神庙） | 将 Golden Goblet（金杯）堆到 Temple（神庙）上并确认召唤。 |
| 7 | Kill the Demon（击杀恶魔） | 击败由 Temple（神庙）召唤的 Demon（恶魔）。这是基础版主线终点。 |

### 5.2 Side Quests（支线任务，37 项）

#### Power & Skill（力量与技巧，3 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Train Militia（训练民兵） | 给 Villager（村民）装备 Spear（长矛），变为 Militia（民兵）。 |
| 2 | Kill a Rat（击杀老鼠） | 在战斗中击败 Rat（老鼠）。 |
| 3 | Kill a Skeleton（击杀骷髅） | 在战斗中击败 Skeleton（骷髅）。 |

#### Potluck（聚餐，4 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Start a Campfire（点燃篝火） | 1 Stick（木棍） + 1 Flint（燧石） → Campfire（篝火）。 |
| 2 | Cook Raw Meat（烹饪生肉） | Raw Meat（生肉） + Campfire（篝火） → Cooked Meat（熟肉）。 |
| 3 | Cook an Omelette（制作煎蛋卷） | 2 Egg（鸡蛋） + Campfire（篝火） → Omelette（煎蛋卷）。 |
| 4 | Cook a Frittata（制作烘蛋） | 1 Egg（鸡蛋） + 1 Potato（土豆） + Campfire（篝火） → Frittata（烘蛋）。 |

#### Discovery（探索发现，7 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Explore a Forest（探索森林） | 用 Villager（村民）完成一次 Forest（森林）探索。 |
| 2 | Explore a Mountain（探索山脉） | 用 Villager（村民）完成一次 Mountain（山脉）探索。 |
| 3 | Open a Treasure Chest（打开宝箱） | Treasure Chest（宝箱） + Key（钥匙）。 |
| 4 | Find a Graveyard（找到墓地） | 2 Corpse（尸体） → Graveyard（墓地）。 |
| 5 | Get a Dog（获得狗） | Wolf（狼） + Bone（骨头） → Dog（狗）。 |
| 6 | Train an Explorer（训练探险家） | 给 Villager（村民）装备 Map（地图），变为 Explorer（探险家）。 |
| 7 | Buy Something from a Travelling Cart（从旅行商车购买物品） | 向 Travelling Cart（旅行商车）支付 5 Coin（金币）。 |

#### Ways and Means（方法与手段，12 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Have 5 Ideas（拥有 5 个点子） | 累计发现 5 张 Idea（点子）；点子可以在发现后出售。 |
| 2 | Have 10 Ideas（拥有 10 个点子） | 累计发现 10 张 Idea（点子）；不要求同时留在场上。 |
| 3 | Have 10 Wood（拥有 10 份木材） | 场上同时拥有 10 张 Wood（木材）。 |
| 4 | Have 10 Stone（拥有 10 块石头） | 场上同时拥有 10 张 Stone（石头）。 |
| 5 | Get an Iron Bar（获得铁锭） | 获得 1 张 Iron Bar（铁锭）；可用 Smelter（冶炼炉）熔炼。 |
| 6 | Have 5 Food（拥有 5 点食物） | 场上全部食物卡的食物点合计至少为 5。 |
| 7 | Have 10 Food（拥有 10 点食物） | 场上食物点合计至少为 10。 |
| 8 | Have 20 Food（拥有 20 点食物） | 场上食物点合计至少为 20。 |
| 9 | Have 50 Food（拥有 50 点食物） | 场上食物点合计至少为 50。 |
| 10 | Have 10 Coins（拥有 10 枚金币） | 同时拥有至少 10 Coin（金币）。 |
| 11 | Have 30 Coins（拥有 30 枚金币） | 同时拥有至少 30 Coin（金币）。 |
| 12 | Have 50 Coins（拥有 50 枚金币） | 同时拥有至少 50 Coin（金币）。 |

#### Construction（建设，7 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Have 3 Houses（拥有三座房屋） | 场上同时拥有 3 张 House（房屋）。 |
| 2 | Build a Shed（建造棚屋） | 1 Stone（石头） + 1 Wood（木材） + 1 Stick（木棍） + 1 Villager（村民） → Shed（棚屋）。 |
| 3 | Build a Quarry（建造采石场） | 3 Stone（石头） + 1 Wood（木材） + 1 Villager（村民） → Quarry（采石场）。 |
| 4 | Build a Lumber Camp（建造伐木场） | 3 Wood（木材） + 1 Stone（石头） + 1 Villager（村民） → Lumber Camp（伐木场）。 |
| 5 | Build a Farm（建造农场） | 1 Soil（土壤） + 2 Brick（砖） + 2 Plank（木板） + 1 Villager（村民） → Farm（农场）。 |
| 6 | Build a Brickyard（建造砖厂） | 1 Brick（砖） + 1 Iron Bar（铁锭） + 1 Wood（木材） + 1 Villager（村民） → Brickyard（砖厂）。 |
| 7 | Sell a Card at a Market（在市场出售卡牌） | 把一张可出售卡堆到 Market（市场）并等待出售完成。 |

#### Longevity（长久生存，4 项）

| # | 任务 | 完成条件 |
|---:|---|---|
| 1 | Reach Moon 6（到达第 6 月） | 在单局中存活到 Moon 6。 |
| 2 | Reach Moon 12（到达第 12 月） | 在单局中存活到 Moon 12。 |
| 3 | Reach Moon 24（到达第 24 月） | 在单局中存活到 Moon 24。 |
| 4 | Reach Moon 36（到达第 36 月） | 在单局中存活到 Moon 36。 |

### 5.3 任务与卡包解锁逻辑

- 任务既是新手引导，也是卡包的进度门槛；完成任意符合条件的任务都会增加累计任务数，不要求严格按表格顺序推进。
- Humble Beginnings（简陋开端）在完成 3 项任务后解锁；后续基础主大陆卡包依次在累计完成 10、16、22、24、26、30、34 项任务时解锁，详见 2.3 节。
- Unlock All Packs（解锁全部卡包）是状态检查任务：基础口径下，累计完成 34 项任务并解锁 Order and Structure（秩序与结构）后即可满足全部基础主大陆卡包的解锁条件。
- 任务完成记录跨局保留，但 Have（拥有）类任务通常检查当前场面状态；其中 Idea（点子）任务按累计发现数计算，而资源、食物、金币、房屋任务要求同时达到目标数量。
- 基础版击败 Demon（恶魔）即完成主线。本节不继续记录其后由免费岛屿更新或其他扩展加入的任务链。

## 6. 资料来源与校验说明

- [Cardopedia/Original](https://stacklands.fandom.com/wiki/Cardopedia/Original)：原始页面列出 123 张卡；本文删除资料跨版本且来源异常的 Filter Crossroads（过滤十字路口）及其点子卡后采用 121 张口径。
- [Ideas](https://stacklands.fandom.com/wiki/Category:Ideas)：点子类别和配方表。
- [Booster Packs](https://stacklands.fandom.com/wiki/Booster_Packs)：卡包价格、卡槽、一次性点子和闪卡规则。
- 各主大陆卡包独立页面：A New World（新世界）、Humble Beginnings（简陋开端）、Seeking Wisdom（寻求智慧）、Reap & Sow（收割与播种）、Curious Cuisine（好奇料理）、Logic and Reason（逻辑与理性）、The Armory（军械库）、Explorers（探险家卡包）、Order and Structure（秩序与结构）、New Weaponry（新式武器）的逐卡槽掉落池。
- [Moon](https://stacklands.fandom.com/wiki/Moon)：月长、喂食和主大陆周期事件。
- [Combat Mechanics](https://stacklands.fandom.com/wiki/Combat_Mechanics)：攻击结算、克制、撤退和战斗属性。
- [Quests](https://stacklands.fandom.com/wiki/Quests)：任务分组、完成条件和任务解锁逻辑；本文仅摘取无需 DLC/扩展地图的基础主大陆任务。
- [卡图颜色改动记录](https://stacklands.fandom.com/f?catId=2951287)：Poop（粪便）改为绿色，0 食物点卡改为浅橙色；用于区分当前卡面与 2022 v1.0 历史颜色。
- 各卡牌独立页面：配方、时间、食物点、战斗属性、地点结果和特殊交互。

Fandom 为社区 Wiki（CC BY-SA），可能存在错字、页面合并或版本延迟。需要实现数值完全一致的复刻时，应再以目标游戏版本的实际运行结果或合法取得的本地数据做最终校验。
