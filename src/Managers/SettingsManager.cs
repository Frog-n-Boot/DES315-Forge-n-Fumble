using Godot;
using System;

public partial class SettingsManager : Node
{
	public static SettingsManager instance {get; private set;}

	//Audio variables
	public float masterVolume = 1.0f;
	public float musicVolume = 1.0f;

	public float SFXVolume = 1.0f;

	//Display vairables
	public bool isFullscreen = false;
	public Vector2I resolution = new Vector2I(1920, 1080);
	public bool VSync = false;
	public bool showFPS = false;
	public int maxFPS = 60;
	private Label fpsLabel;

	public override void _Ready()
	{
		instance = this;
		LoadSettings();
		ApplySettings();
		CreateFPSLabel();
	}

    public override void _Process(double delta)
    {
        if(showFPS && fpsLabel != null)
		{
			fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
		}
    }

	private void CreateFPSLabel()
	{
		fpsLabel = new Label();
		fpsLabel.AnchorLeft = 1.0f;
		fpsLabel.AnchorRight = 1.0f;
		fpsLabel.AnchorTop = 0.0f;
		fpsLabel.GrowHorizontal = Control.GrowDirection.Begin;
		fpsLabel.Position = new Vector2(-30.0f, 100.0f);
		fpsLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		fpsLabel.Visible = showFPS;
		AddChild(fpsLabel);
	}
	private void SetWindowPosition()
	{
			Vector2I screenSize = DisplayServer.ScreenGetSize();
			Vector2I centeredPos = new Vector2I((screenSize.X - resolution.X) / 2, (screenSize.Y - resolution.Y) / 2);
			DisplayServer.WindowSetPosition(centeredPos);
	}

	public void ApplySettings()
	{
		//Audio
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(masterVolume));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb(musicVolume));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb(SFXVolume));

		//Display
		if (isFullscreen)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.WindowSetSize(resolution);
			SetWindowPosition();
		}

		DisplayServer.VSyncMode vsyncMode = VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled;
		DisplayServer.WindowSetVsyncMode(vsyncMode);

		Engine.MaxFps = maxFPS;
	}

	public void SetMasterVolume(float value)
	{
		masterVolume = value;
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(value));
	}

	public void SetMusicVolume(float value)
	{
		musicVolume = value;
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb(value));
	}

	public void SetSFXVolume(float value)
	{
		SFXVolume = value;
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb(value));
	}

	public void SetFullScreen(bool value)
	{
		isFullscreen = value;
		if (value)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			Engine.MaxFps = maxFPS;
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.WindowSetSize(resolution);
			SetWindowPosition();
		}
	}

	public void SetResolution(Vector2I Resolution)
	{
		resolution = Resolution;
		if(!isFullscreen)
		{
			DisplayServer.WindowSetSize(Resolution);
			SetWindowPosition();
		}
	}

	public void SetVSync(bool value)
	{
		VSync = value;
		DisplayServer.WindowSetVsyncMode(value ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
	}

	public void SetShowFPS(bool value)
	{
		showFPS = value;

		if(fpsLabel != null) fpsLabel.Visible = value;

		
	}

	public void SetMaxFPS(long value)
	{
		maxFPS = (int)value;
		Engine.MaxFps = (int)value;
	}

	public void SaveSettings()
	{
		var config = new ConfigFile();
		config.SetValue("Audio", "MasterVolume", masterVolume);
		config.SetValue("Audio", "MusicVolume", musicVolume);
		config.SetValue("Audio", "SFXVolume", SFXVolume);
		config.SetValue("Display", "IsFullscreen", isFullscreen);
		config.SetValue("Display", "Resolution", resolution);
		config.SetValue("Display", "VSync", VSync);
		config.SetValue("Display", "ShowFPS", showFPS);
		config.SetValue("Display", "MaxFPS", maxFPS);
		config.Save("user://settings.cfg");
	}

	public void LoadSettings()
	{
		var config = new ConfigFile();
		if(config.Load("user://settings.cfg") != Error.Ok) return;

		masterVolume = (float)config.GetValue("Audio", "MasterVolume", 1.0f);
		musicVolume = (float)config.GetValue("Audio", "MusicVolume", 1.0f);
		SFXVolume = (float)config.GetValue("Audio", "SFXVolume", 1.0f);
		isFullscreen = (bool)config.GetValue("Display", "IsFullscreen", false);
		resolution = (Vector2I)config.GetValue("Display", "Resolution", new Vector2I(1920, 1080));
		VSync = (bool)config.GetValue("Display", "VSync", false);
		showFPS = (bool)config.GetValue("Display", "ShowFPS", false);
		maxFPS = (int)config.GetValue("Display", "MaxFPS", 60);

		//ApplySettings();
	}
}
