# GameProject lib

将需要引用的 DLL 放入此目录，GameEngine、GameLogic 和 GameEngineEditor 工程会自动引用 `GameProject/lib` 下的所有 `.dll`。

## editor 子目录

`GameEngineEditor` 工程还会引用 `GameProject/lib/editor/` 目录下的所有 `.dll`（仅当该目录存在时）。

请将 Unity 编辑器相关 DLL 放入此目录，例如：

- `UnityEditor.dll`
- `UnityEditor.CoreModule.dll`

这些文件通常位于 Unity 安装目录下，例如：

```
C:\Program Files\Unity\Hub\Editor\<版本号>\Editor\Data\Managed\UnityEditor.dll
```
