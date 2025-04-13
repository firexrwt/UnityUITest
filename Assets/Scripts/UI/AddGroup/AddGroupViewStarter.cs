namespace App.UI
{
    public class AddGroupViewStarter : IWindowStarter
    {
        public string GetGroup() => "AddGroup";
        public string GetName() => "AddGroupView";

        public void SetupModels(ViewController viewController)
        {

        }
    }
}