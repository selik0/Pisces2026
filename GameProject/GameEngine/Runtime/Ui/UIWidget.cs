namespace GameEngine
{
    /// <summary>
    /// 可复用 UI 组件基类。
    /// 与 UIView 使用同一套生命周期，并可通过 UIBrick 的组合能力嵌套其他界面或组件。
    /// </summary>
    public abstract class UIWidget : UIBrick
    {
    }
}
