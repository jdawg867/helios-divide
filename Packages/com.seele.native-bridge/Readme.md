# Native Bridge
代码核心逻辑位于 com.seele.native-bridge\Runtime\Scripts\SeeleLifeCycleManager.cs

开始埋点
```c#
// res 表示是否展示了广告
var res = await SeeleLifeCycleManager.StartGame();
```

结束埋点
```c#
// res 表示是否展示了广告
var res = await SeeleLifeCycleManager.GameOver();
```

激励广告
```c#
// res 表示是否获得激励
var res = await SeeleLifeCycleManager.StartGame();
```
