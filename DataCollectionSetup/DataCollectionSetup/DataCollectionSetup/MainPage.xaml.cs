using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DataCollectionSetup
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        #region Constructor

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        #endregion

        #region Command Bar Buttons (Open, Save)

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear session if user confirms
            var title = "Please confirm...";
            var content = "Are you sure that you want to clear your sessions?";
            ClearSession(title, content);
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // clear session if user confirms
            var title = "Please confirm...";
            var content = "Do you want to load new file without saving? Loading new file will clear your entire current session.";
            Task task = ClearSession(title, content);
            await task;

            // get the file
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".xml");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null){ return; }

            // load XML file to session
            ReadFromXml(file);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // get the title and randomization
            string title = MyTitleText.Text;
            bool isRandom = MyRandomizeToggle.IsOn;

            // validate title
            if (title.Equals(""))
            {
                var dialog = new MessageDialog("ERROR: You are missing the title.");
                await dialog.ShowAsync();
                return;
            }

            // get the prompts
            var promptElements = MyPromptElementsStack.Children;

            // validate the prompt size
            if (promptElements.Count == 0)
            {
                var dialog = new MessageDialog("ERROR: You have no prompts.");
                await dialog.ShowAsync();
                return;
            }

            // iterate through each prompt
            var promptElementDataList = new List<Tuple<string, string, int, string>>();
            for (int i = 0; i < promptElements.Count; ++i)
            {
                // get the current prompt
                PromptElement promptElement = (PromptElement)promptElements[i];

                // get the prompt's contents
                string imageFileName = promptElement.ImageFileName;
                string labelName = promptElement.LabelName;
                string iterationsText = promptElement.IterationsText;
                string displayType = promptElement.IsDisplayTrace ? "trace" : promptElement.IsDisplayReference ? "reference" : "none";

                // validate the prompt's contents
                bool isValid = true;
                string errorMessage = "";
                int iterations = 1;
                if (imageFileName.Equals("") && !displayType.Equals("none"))
                {
                    isValid = false;
                    errorMessage = "ERROR: One of your prompts is missing an image file name.";
                }
                else if (labelName.Equals(""))
                {
                    isValid = false;
                    errorMessage = "ERROR: One of your prompts is missing a label name.";
                }
                else if (!int.TryParse(iterationsText, out iterations))
                {
                    isValid = false;
                    errorMessage = "ERROR: One of your prompts uses a non-number for iterations.";
                }
                else if (iterations <= 0)
                {
                    isValid = false;
                    errorMessage = "ERROR: One of your prompts uses a non-positive number for iterations.";
                }
                if (!isValid)
                {
                    var dialog = new MessageDialog(errorMessage);
                    await dialog.ShowAsync();
                    return;
                }

                // get the prompt element's data
                var promptElementData = new Tuple<string, string, int, string>(imageFileName, labelName, iterations, displayType);
                promptElementDataList.Add(promptElementData);
            }

            // get save file
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("eXtensible Markup Language (XML) file", new List<string>() { ".xml" });
            savePicker.SuggestedFileName = "New Document";
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file == null) { return; }

            // write to XML file
            WriteToXml(file, title, isRandom, promptElementDataList);

            // show success message dialog
            var successDialog = new MessageDialog("File saved successfully.", "Success");
            await successDialog.ShowAsync();
        }

        #endregion

        #region Modifying Buttons (Add, Remove, Insert)

        private void AddPromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            PromptElement currentPromptElement = new PromptElement(MyPromptElementsStack.Children.Count, MyCounter);
            currentPromptElement.Name = "myPromptElement" + MyCounter;
            MyCounter++;

            MyPromptElementsStack.Children.Add(currentPromptElement);
        }

        private void RemovePromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            var promptElementsToDelete = new List<PromptElement>();
            foreach (PromptElement promptElement in MyPromptElementsStack.Children)
            {
                if (promptElement.IsChecked)
                {
                    promptElementsToDelete.Add(promptElement);
                }
            }

            if (promptElementsToDelete.Count == 0) { return; }

            foreach (var promptElementToDelate in promptElementsToDelete)
            {
                MyPromptElementsStack.Children.Remove(promptElementToDelate);
            }

            for (int i = 0; i < MyPromptElementsStack.Children.Count; ++i)
            {
                var promptElement = (PromptElement)(MyPromptElementsStack.Children[i]);
                promptElement.PositionName = "" + i;
            }
        }

        private void InsertPromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            // check if the value is valid
            string value = MyInsertIndexText.Text;
            int index;
            if (!int.TryParse(value, out index)) {
                MyInsertIndexText.Text = ""; // clear the text box
                return; // do nothing
            }

            //
            int count = MyPromptElementsStack.Children.Count;

            // case: stack is empty
            if (count == 0)
            {
                PromptElement currentPromptElement = new PromptElement(0, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                MyPromptElementsStack.Children.Add(currentPromptElement);
            }

            // case: index exceeds stack size
            else if (index >= count)
            {
                PromptElement currentPromptElement = new PromptElement(count, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                MyPromptElementsStack.Children.Add(currentPromptElement);
            }

            //
            else
            {
                PromptElement currentPromptElement = new PromptElement(index, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                //
                MyPromptElementsStack.Children.Insert(index, currentPromptElement);
                for (int i = index + 1; i < MyPromptElementsStack.Children.Count; ++i)
                {
                    var promptElement = (PromptElement)MyPromptElementsStack.Children[i];
                    promptElement.PositionName = "" + i;
                }
            }

            this.MyInsertIndexText.Text = "";
        }

        #endregion

        #region Helper Methods

        private async Task ClearSession(string title, string content)
        {
            // show warning dialog
            MessageDialog dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new UICommand("No") { Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();
            if ((int)result.Id == 0) {; }
            else { return; }

            // clear session
            MyCounter = 0;
            MyTitleText.Text = "";
            MyPromptElementsStack.Children.Clear();
            MyRandomizeToggle.IsOn = false;
            MyInsertIndexText.Text = "";
        }

        private void ReadFromXml(StorageFile file)
        {
            using (XmlReader reader = XmlReader.Create(file.Path))
            {
                reader.ReadAsync();
            }
        }

        private async void WriteToXml(StorageFile file, string title, bool isRandom, List<Tuple<string, string, int, string>> prompts)
        {
            // create the string writer as the streaming source of the XML data
            string output = "";
            using (StringWriter stringWriter = new StringWriter())
            {
                // set the string writer as the streaming source for the XML writer
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                {
                    // <xml>
                    xmlWriter.WriteStartDocument();

                    // <DataCollection>
                    xmlWriter.WriteStartElement("DataCollection");
                    xmlWriter.WriteAttributeString("title", title);
                    xmlWriter.WriteAttributeString("Random", "" + isRandom);

                    // iterate through each stroke
                    foreach (var prompt in prompts)
                    {
                        // <Prompt>
                        xmlWriter.WriteStartElement("Prompt");

                        xmlWriter.WriteAttributeString("image", prompt.Item1);
                        xmlWriter.WriteAttributeString("label", prompt.Item2);
                        xmlWriter.WriteAttributeString("iterations", "" + prompt.Item3);
                        xmlWriter.WriteAttributeString("display", prompt.Item4);

                        // </Prompt>
                        xmlWriter.WriteEndElement();
                    }

                    // </DataCollection>
                    xmlWriter.WriteEndElement();

                    // </xml>
                    xmlWriter.WriteEndDocument();
                }

                output = stringWriter.ToString();
            }

            await FileIO.WriteTextAsync(file, output);
        }

        #endregion

        #region Fields

        private int MyCounter { get; set; }

        #endregion
    }
}
