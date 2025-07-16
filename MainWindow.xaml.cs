using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using Gma.System.MouseKeyHook;


namespace KhaledsAutoClicker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Import mouse_event function
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private bool isWaitingForKey = false;

    private IKeyboardMouseEvents globalHook;
    private Key boundKey = Key.None;
    private int KeyCode = -1;

    private bool isAutoClickerActive = false;
    private CancellationTokenSource _clickTokenSource;

    private static readonly Random random = new Random();

    public MainWindow()
    {
        InitializeComponent();
        this.KeyDown += MainWindow_KeyDown;
        SubscribeGlobalHook();
        this.Closed += MainWindow_Closed; // Clean up hook on close
    }

    private void SubscribeGlobalHook()
    {
        globalHook = Hook.GlobalEvents();
        globalHook.KeyDown += GlobalHook_KeyDown;
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        globalHook?.Dispose();
    }

    private void btnBindKey_Click(object sender, RoutedEventArgs e)
    {
        isWaitingForKey = true;
        txtBoundKey.Text = "Waiting for key...";
        btnBindKey.Content = "Press any key...";
        Keyboard.Focus(this); // Ensure window has focus to capture key input
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (!isWaitingForKey)
            return;

        // Capture the pressed key
        Key pressedKey = e.Key;

        // Special handling for Shift/Alt/etc. (optional)
        if (pressedKey == Key.System)
            pressedKey = e.SystemKey;

        boundKey = pressedKey; // Store the bound key for later use
        KeyCode = KeyInterop.VirtualKeyFromKey(boundKey);

        txtBoundKey.Text = $"Bound to: {boundKey.ToString()}";
        btnBindKey.Content = "Click to Bind Key";
        isWaitingForKey = false;
    }

    private void PerformMouseClick()
    {
        // Simulate left mouse button down and up at current cursor position
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    private void GlobalHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        // early return when key binding is not set
        if (KeyCode == -1 || boundKey == Key.None)
            return;

        // Convert WinForms key to WPF Key enum
        Key pressedKey = KeyInterop.KeyFromVirtualKey((int)e.KeyCode);

        if (pressedKey == boundKey)
        {
            // Error control based on random or regular clicking
            if (chkRandomize.IsChecked == true)
            {
                int.TryParse(randomizeRangeMin.Text, out int delayMin);
                int.TryParse(randomizeRangeMax.Text, out int delayMax);

                // Double check to see if the delay value is set
                if (delayMin < 100)
                {
                    MessageBox.Show("Please enter a valid positive number! Minimum must be 100ms or more", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (delayMax <= delayMin)
                {
                    MessageBox.Show("Max must be greater than Min", "Invalid Input", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // Double check to see if the delay value is set
                if (!int.TryParse(clickDelayInterval.Text, out int delay) || delay < 100)
                {
                    MessageBox.Show("Please enter a valid positive number! Minimum must be 100ms or more", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            isAutoClickerActive = !isAutoClickerActive; // Toggle auto-clicker state

            if (isAutoClickerActive)
            {
                if (chkRandomize.IsChecked == true)
                {
                    StartRandomClicking();
                }
                else
                {
                    StartClicking();
                }
            }
            else
            {
                StopClicking();
            }
        }
    }

    private async void StartClicking()
    {
        autoclickTextBlock.Text = "Auto Clicker is Active";
        autoclickTextBlock.Foreground = System.Windows.Media.Brushes.Green;

        _clickTokenSource = new CancellationTokenSource();
        var token = _clickTokenSource.Token;
        int.TryParse(clickDelayInterval.Text, out int delay);

        await Task.Run(async () =>
        {
            while (isAutoClickerActive && !token.IsCancellationRequested)
            {
                PerformMouseClick();
                await Task.Delay(delay);
            }
        }, token);
    }

    private async void StartRandomClicking()
    {
        autoclickTextBlock.Text = "Auto Clicker is Active";
        autoclickTextBlock.Foreground = System.Windows.Media.Brushes.Green;

        _clickTokenSource = new CancellationTokenSource();
        var token = _clickTokenSource.Token;
        int delay = -1;
        int.TryParse(randomizeRangeMin.Text, out int randRangeMin);
        int.TryParse(randomizeRangeMax.Text, out int randRangeMax);

        await Task.Run(async () =>
        {
            while (isAutoClickerActive && !token.IsCancellationRequested)
            {
                PerformMouseClick();
                delay = random.Next(randRangeMin, randRangeMax);
                await Task.Delay(delay);
            }
        }, token);
    }

    private void StopClicking()
    {
        autoclickTextBlock.Text = "Auto Clicker is Inactive";
        autoclickTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        _clickTokenSource?.Cancel();
    }

    private void OnlyAllowNumbers(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private bool IsTextNumeric(string text)
    {
        return int.TryParse(text, out _);
    }

    private void chkRandomize_Checked(object sender, RoutedEventArgs e)
    {
        RegularPanel.Visibility = Visibility.Hidden;
        RandomizePanel.Visibility = Visibility.Visible;
    }

    private void chkRandomize_Unchecked(object sender, RoutedEventArgs e)
    {
        RegularPanel.Visibility = Visibility.Visible;
        RandomizePanel.Visibility = Visibility.Hidden;
    }
}