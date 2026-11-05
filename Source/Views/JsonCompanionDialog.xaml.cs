using System.Windows;

namespace WatchedFilmsTracker.Source.Views;

public partial class JsonCompanionDialog : Window
{
    public enum Choice
    {
        Cancel,
        Primary,
        Secondary
    }

    public Choice Result { get; private set; } = Choice.Cancel;

    public JsonCompanionDialog(bool creatingNewCollection, string companionPath = "")
    {
        InitializeComponent();
        CompanionPathTextBox.Text = companionPath;
        CompanionPathTextBox.Visibility = creatingNewCollection ? Visibility.Collapsed : Visibility.Visible;

        if (creatingNewCollection)
        {
            Title = "JSON for new collection";
            DescriptionText.Text = "Create a JSON companion file for this collection? It stores collection metadata, statistics and refresh preferences. The JSON file will be created beside the CSV when you save the collection.";
            PrimaryButton.Content = "Create JSON when saving";
            SecondaryButton.Content = "Use CSV only";
            PrimaryButton.IsDefault = true;
        }
        else
        {
            DescriptionText.Text = "Turn off JSON storage for this collection. Choose whether to delete the existing companion file or keep it on disk. Your current statistics will remain available until you close the collection.";
            PrimaryButton.Content = "Delete JSON file";
            SecondaryButton.Content = "Keep JSON file";
            SecondaryButton.IsDefault = true;
        }
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = Choice.Primary;
        DialogResult = true;
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = Choice.Secondary;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = Choice.Cancel;
        DialogResult = false;
    }
}
