namespace Game
{
    public class MyTipDialog : Dialog
    {

        public BevelledButtonWidget bevelledButtonWidget = new BevelledButtonWidget();
        public LabelWidget labelWidget = new LabelWidget();
        public CanvasWidget canvasWidget = new CanvasWidget();
        public MyTipDialog(string text,string cancel) {
            Children.Add(canvasWidget);
            StackPanelWidget stackPanel = new StackPanelWidget() { HorizontalAlignment=WidgetAlignment.Center,VerticalAlignment=WidgetAlignment.Center,Direction=LayoutDirection.Vertical};
            bevelledButtonWidget.Text = cancel;
            labelWidget.Text = text;
            stackPanel.Children.Add(labelWidget);
            canvasWidget.Children.Add(stackPanel);
        }
        public override void Update()
        {
            if (bevelledButtonWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }

    }
}