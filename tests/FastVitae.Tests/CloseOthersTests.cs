using System.Windows;
using FastVitae.Views;
using Xunit;

namespace FastVitae.Tests;

[Collection("Windows")]
public class CloseOthersTests
{
    // WPF windows need STA
    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null)
            throw new Xunit.Sdk.XunitException(error.ToString());
    }

    // Handle goes on close
    private static bool IsOpen(Window window) => new System.Windows.Interop.WindowInteropHelper(window).Handle != IntPtr.Zero;

    // Keep, owner, owned by owner
    private static List<Window> ShowThree()
    {
        var all = new List<Window> { new(), new(), new() };
        all[0].Show();
        all[1].Show();
        all[2].Owner = all[1];
        all[2].Show();
        return all;
    }

    [Fact]
    public void OwnedWindowCancels_OwnerStays() => OnSta(() =>
    {
        var all = ShowThree();
        bool cancel = true;
        all[2].Closing += (s, e) => e.Cancel = cancel;

        Assert.False(HomeWindow.CloseOthers(all[0], all));
        Assert.True(IsOpen(all[2]));
        Assert.True(IsOpen(all[1]));

        cancel = false;
        all[2].Close();
        all[1].Close();
        all[0].Close();
    });

    [Fact]
    public void NobodyCancels_AllClosed() => OnSta(() =>
    {
        var all = ShowThree();

        Assert.True(HomeWindow.CloseOthers(all[0], all));
        Assert.False(IsOpen(all[1]));
        Assert.False(IsOpen(all[2]));
        Assert.True(IsOpen(all[0]));
        all[0].Close();
    });
}
