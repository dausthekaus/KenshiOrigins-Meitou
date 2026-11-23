using System.Numerics;
using Meitou.Data.Ui;
using Meitou.Game.Rendering.Frame;
using Meitou.Game.Rendering.Gpu;
using Meitou.Game.Rendering.Gpu.Core;
using Meitou.Game.Rendering.Overlays;
using Meitou.Game.Rendering.Ui;

/// <summary>Optional offscreen image made with the release's actual painter, skins and fonts.</summary>
internal static class UiPreview
{
    internal static void Save(MyGuiResources resources, Func<string, string?> resolve,
        Func<UiPainter, IReadOnlyList<UiControl>> arrange, string path, int width, int height)
    {
        using var device = VulkanDevice.Create(new VulkanDeviceOptions());
        using var gpu = new GpuContext(device);
        using var overlay = DebugOverlay.TryCreate(gpu, resolve) ?? throw new InvalidOperationException("No preview font available.");
        using var target = gpu.CreateTexture(new TextureDesc(InternalFormat.Rgba8, width, height,
            Use: TextureUse.ColourTarget | TextureUse.TransferSrc));
        var cmd = gpu.BeginNative("clear origin UI preview");
        var targets = PassTargets.Of(target, null);
        cmd.BeginRendering(targets.Rendering with { Colour = targets.Colour with { Load = LoadOp.Clear, Clear = new ClearColour(.13f, .16f, .17f, 1) } });
        cmd.EndRendering();
        gpu.EndNative(cmd);
        overlay.Target = target;
        using var painter = new UiPainter(overlay, resources, resolve);
        painter.Draw(arrange(painter), width, height, new Vector2(-1, -1), null);
        gpu.Finish();
        FramebufferCapture.SavePng(gpu, target, path, width, height);
        Console.WriteLine("UI preview: " + path);
    }
}
