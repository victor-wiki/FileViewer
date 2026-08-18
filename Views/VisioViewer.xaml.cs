using FileViewer.Manager;
using Newtonsoft.Json;
using VisioConverter.Converter;
using VisioConverter.Model;

namespace FileViewer.Views;

public partial class VisioViewer : ContentPage
{
    private string filePath;
    private Stream stream;
    private ConvertResult result;
    private int pageCount = 0;
    private bool isLoading = false;
    private bool isPageLoaded = false;
    private double? windowWidth = null;
    private double? pageWidth = null;
    private int currentZoomPercent = 100;

    public VisioViewer(string filePath)
    {
        InitializeComponent();

        this.filePath = filePath;

        this.lblTitle.Text = Path.GetFileName(filePath);
    }

    public VisioViewer(Stream stream, string title)
    {
        InitializeComponent();

        this.stream = stream;

        this.lblTitle.Text = title;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        this.windowWidth = (double)Application.Current.MainPage.Window.Width;

        if (!this.isPageLoaded)
        {
            this.viewer.Navigated += this.Viewer_Navigated;              

            this.viewer.Source = new HtmlWebViewSource() { Html = "" };
        }
    }

    private void SetToolbarItemStatus(ToolbarItem item, bool enable)
    {
        FontImageSource fs = item.IconImageSource as FontImageSource;
        fs.Color = enable ? Colors.DodgerBlue : Colors.Transparent;

        item.IsEnabled = enable;
    }   

    public async void ShowPowerPoint()
    {
        bool isMobile = this.IsMobile();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            this.SetToolbarItemStatus(this.tbiZoomIn, false);
            this.SetToolbarItemStatus(this.tbiZoomOut, false);
        });     

        ConvertOption option = new ConvertOption()
        {
            EnableLog = SettingManager.GetSetting().EnableLog,
            DefaultLogFolder = LogManager.LogFolder
        };

        Visio2Html converter = this.stream != null ? new Visio2Html(this.stream, option) : new Visio2Html(this.filePath, option);

        converter.OnPageBeginConvert += this.Converter_OnPageBeginConvert;
        converter.OnPageEndConvert += this.Converter_OnPageEndConvert;
        converter.OnPageConvertError += this.Converter_OnPageConvertError;

        this.result = converter.Convert();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (this.result.IsOK == false)
            {
                LogManager.LogError(this.result.Message);

                await DisplayAlert("Error", this.result.Message, "OK");
            }

            if (this.result.Infos != null && this.result.Infos.Count > 0)
            {
                this.SetToolbarItemStatus(this.tbiZoomIn, true);
                this.SetToolbarItemStatus(this.tbiZoomOut, true);

                this.pageCount = this.result.Infos.Count;

                this.lblTotal.Text = this.pageCount.ToString();

                for (int i = 1; i <= this.pageCount; i++)
                {
                    this.pickerPageNumber.Items.Add(i.ToString());
                }

                this.pickerPageNumber.SelectedIndex = 0;
            }

            this.lblMessage.Text = "";
            this.lblMessage.IsVisible = false;
            this.MainGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Absolute);
        });
    }

    private async void Viewer_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        if (!this.isPageLoaded)
        {
            this.isPageLoaded = true;

            await Task.Run(() =>
            {
                this.ShowPowerPoint();
            });
        }
    }

    private void ShowMessage(string message, bool isError = false)
    {
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            this.lblMessage.Text = message;
            this.lblMessage.TextColor = isError ? Colors.Red : Colors.Black;
        });
    }

    private void Converter_OnPageConvertError(int pageIndex, string message)
    {
        this.ShowMessage($"Error occurs when convert page{(pageIndex + 1)}:{message}", true);
    }

    private void Converter_OnPageEndConvert(int pageIndex, HtmlConvertInfo info)
    {
        this.ShowMessage($"End convert page{(pageIndex + 1)}.");
    }

    private void Converter_OnPageBeginConvert(int pageIndex)
    {
        this.ShowMessage($"Start to convert page{(pageIndex + 1)}...");
    }

    private void btnFirst_Clicked(object sender, EventArgs e)
    {
        this.ShowHtml(0);
    }

    private void btnPrevious_Clicked(object sender, EventArgs e)
    {
        this.ShowHtml(this.pickerPageNumber.SelectedIndex - 1);
    }

    private void btnNext_Clicked(object sender, EventArgs e)
    {
        this.ShowHtml(this.pickerPageNumber.SelectedIndex + 1);
    }

    private void btnLast_Clicked(object sender, EventArgs e)
    {
        this.ShowHtml(this.pageCount - 1);
    }

    private bool IsMobile()
    {
        return DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS;
    }

    private async void ShowHtml(int index)
    {
        if (index >= 0 && index < this.pageCount)
        {
            try
            {
                var info = this.result.Infos[index];

                var html = $"<div id='container' style='overflow:visible;'>{info.Html}</div>" ;

                this.pageWidth = info.Width;

                bool isMobile = this.IsMobile();                             

                if (isMobile)
                {
                    this.viewer.Source = new HtmlWebViewSource() { Html = html };
                }
                else
                {
                    this.viewer.Source = new Uri("about:blank");

                    this.WriteHtml(html);
                }               
            }
            catch (Exception ex)
            {
                this.ClearContent();
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                this.isLoading = true;

                this.pickerPageNumber.SelectedIndex = index;

                this.isLoading = false;

                this.SetControlStatus(index);
            }
        }
    }

    private async void WriteHtml(string html)
    {
        if (this.viewer.IsLoaded)
        {
            string encodedHtml = JsonConvert.SerializeObject(html);
            string script = "window.document.write(" + encodedHtml + ")";

            await this.viewer.EvaluateJavaScriptAsync(script);
        }
    }

    private void SetControlStatus(int index)
    {
        this.btnFirst.IsEnabled = index > 0;
        this.btnPrevious.IsEnabled = index > 0;
        this.btnNext.IsEnabled = index < this.pageCount - 1;
        this.btnLast.IsEnabled = index < this.pageCount - 1;
    }

    private void ClearContent()
    {
        this.viewer.Source = new HtmlWebViewSource { Html = "" };
    }

    private void pickerPageNumber_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (this.isLoading)
        {
            return;
        }

        int index = this.pickerPageNumber.SelectedIndex;

        this.ShowHtml(index);
    }

    private void tbiZoomIn_Clicked(object sender, EventArgs e)
    {
        this.SetZoom(true);
    }

    private void tbiZoomOut_Clicked(object sender, EventArgs e)
    {
        this.SetZoom(false);
    }

    private async void SetZoom(bool isZoomIn)
    {
        int currentZoomPercent = this.currentZoomPercent;

        if(isZoomIn)
        {
            currentZoomPercent += 10;
        }
        else
        {
            currentZoomPercent -= 10;
        }

        this.currentZoomPercent = currentZoomPercent;

        var scale = Math.Round(currentZoomPercent / 100.0, 2);
        var currentWidth = this.pageWidth * scale;

        
        await this.viewer.EvaluateJavaScriptAsync($"var svg =document.getElementsByTagName('svg')[0]; svg.style.transform='scale({scale})'; svg.style.transformOrigin = '0 0'; svg.style.transformBox ='fill-box'; document.getElementById('container').style.width='{currentWidth}px';");
    }
}