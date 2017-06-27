using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DataCollectionSetup
{
    public sealed partial class PromptElement : UserControl
    {
        public PromptElement(int position, int count)
        {
            this.InitializeComponent();

            //
            this.MyPositionText.Text = "" + position;

            //
            this.MyTraceButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
            this.MyReferenceButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
            this.MyNoneButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
        }

        private async void MyReviewImageFileNameButton_Click(object sender, RoutedEventArgs e)
        {
            //
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");

            //
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) { return; }

            MyImageFileNameText.Text = file.Name;

        }

        public bool IsChecked { get { return MyRemoveCheckBox.IsChecked.Value; } }
        public string PositionName { get { return MyPositionText.Text; } set { MyPositionText.Text = value; } }
        public string ImageFileName {  get { return MyImageFileNameText.Text; } }
        public string LabelName { get { return MyLabelText.Text; } }
        public string IterationsText { get { return MyIterationsText.Text; } }
        public bool IsDisplayTrace { get { return MyTraceButton.IsChecked.Value; } }
        public bool IsDisplayReference { get { return MyReferenceButton.IsChecked.Value; } }
        public bool IsDisplayNone { get { return MyNoneButton.IsChecked.Value; } }

        public static readonly String DISPLAY_GROUP_NAME = "DisplayGroup";
    }
}
