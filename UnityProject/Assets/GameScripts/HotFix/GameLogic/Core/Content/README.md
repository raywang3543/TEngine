# Stacklands Original 内容读取层

配置源位于 `Configs/GameConfig/Datas/stacklands_*.xlsx`。修改 Excel 后运行：

```bash
cd Configs/GameConfig
./gen_code_bin_to_project_lazyload.sh
```

生成的 `GameConfig.stacklands.*` 只允许在 `StacklandsContentLoader` 中出现。玩法系统依赖
`IStacklandsContentCatalog`，不要直接读取 Luban Bean。

```csharp
Tables tables = ConfigSystem.Instance.Tables;
ContentValidationReport report = StacklandsContentLoader.Validate(tables);
IStacklandsContentCatalog content = StacklandsContentLoader.Build(tables);

CoreSystem.Initialize(content);

CardDefinition berry = content.Cards.Get("berry");
IReadOnlyList<RecipeDefinition> brickRecipes = content.Recipes.GetByResult("brick");
BoosterDefinition pack = content.Boosters.Get("humble_beginnings");
LootPoolDefinition pool = content.LootPools.Get(pack.Slots[0].NormalPoolId);
ContentRecordDefinition cowStun = content.Effects.Get("effect_cow_stun");
WorldRules rules = content.WorldRules;
```

当前 Original 数据包含 121 张卡、69 条合成/转换配方、56 个掉落池、295 个掉落条目、
33 个卡牌动作、6 个特殊效果、56 项任务和 10 种卡包。全部运行必需权重和世界初始卡牌上限
均已补齐，因此 `Validate` 应返回空报告，
所有掉落池的 `CanRoll` 都应为 `true`。推定数值在 Excel 的 `verify_status` 中标记为 `Partial`，
并在 `source_note` 说明为玩法推定值。引用缺失、数量口径错误和卡包卡槽数量不一致仍属于 error，
`Build` 会拒绝构造目录；未来若再次引入缺失必需值，`RequireWeight` 等防御 API 仍会给出明确来源。
