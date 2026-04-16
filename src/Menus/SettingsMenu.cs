using Godot;
using System;

public partial class SettingsMenu : CanvasLayer
{
	[Export] public HSlider masterSlider;
	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;
	[Export] private CheckBox fullScreenCheckBox;
	[Export] private CheckBox vsyncCheckBox;
	[Export] private CheckBox showFPSCheckBox;
	[Export] private OptionButton resolutionDropdown;
	[Export] private OptionButton maxFPSDropdown;
	[Export] private Button applyButton;
	[Export] private AudioStreamPlayer2D audio;

	[Export] private PauseMenu pauseMenu;

	public bool isInGame = false;

	private Vector2I[] resolution =
	{
		new Vector2I(640, 480),
		new Vector2I(800, 600),
		new Vector2I(1024, 768),
		new Vector2I(1280, 800),
		new Vector2I(1360, 768),
		new Vector2I(1366, 768),
		new Vector2I(1440, 900),
		new Vector2I(1536, 864),
		new Vector2I(1600, 900),
		new Vector2I(1680, 1050),
		new Vector2I(1600, 1200),
		new Vector2I(1920, 1080),
		new Vector2I(1920, 1200),
		new Vector2I(2560, 1440),
		new Vector2I(3840, 2160)
	};

	private int[] fpsOptions = { 30, 60, 120, 144, 240, 0};

	public override void _Ready()
	{
		foreach(var res in resolution)
			resolutionDropdown.AddItem($"{res.X}x{res.Y}");
		foreach (var fps in fpsOptions)
			maxFPSDropdown.AddItem(fps == 0 ? "Unlimited" : $"{fps} FPS");
		
		var s = SettingsManager.instance;

		for(int i = 0; i < resolution.Length; i++)
		{
			if(resolution[i] == s.resolution)
			{
				resolutionDropdown.Selected = i;
				break;
			}
		}

		Vector2I screenRes = DisplayServer.ScreenGetSize();
		Vector2 targetRes = s.resolution == Vector2I.Zero? screenRes : s.resolution;

		int closestIndex = 0;
		int closestDistance = int.MaxValue;

		for(int i = 0; i < resolution.Length; i++)
		{
			int distance = Math.Abs(resolution[i].X - (int)targetRes.X) + Math.Abs(resolution[i].Y - (int)targetRes.Y);

			if(distance < closestDistance)
			{
				closestDistance = distance;
				closestIndex = i;
			}
		}
		resolutionDropdown.Selected = closestIndex;
		SettingsManager.instance.SetResolution(resolution[closestIndex]);

		for(int i = 0; i < fpsOptions.Length; i++)
		{
			if(fpsOptions[i] == s.maxFPS)
			{
				maxFPSDropdown.Selected = i;
				break;
			}
		}

		masterSlider.Value = s.masterVolume;
		musicSlider.Value = s.musicVolume;
		sfxSlider.Value = s.SFXVolume;
		fullScreenCheckBox.ButtonPressed = s.isFullscreen;
		vsyncCheckBox.ButtonPressed = s.VSync;
		showFPSCheckBox.ButtonPressed = s.showFPS;


		masterSlider.ValueChanged += OnMasterVolumeChanged;
		musicSlider.ValueChanged += OnMusicVolumeChanged;
		sfxSlider.ValueChanged += OnSFXVolumeChanged;
		fullScreenCheckBox.Toggled += OnFullScreenToggled;
		vsyncCheckBox.Toggled += OnVSyncToggled;
		showFPSCheckBox.Toggled += OnShowFPSToggled;
		resolutionDropdown.ItemSelected +=OnResolutionSelected;
		maxFPSDropdown.ItemSelected += OnMaxFPSSelected;
		applyButton.Pressed += OnApplyPressed;

		applyButton.GrabFocus();
	}

	public void OnMasterVolumeChanged( double value) => SettingsManager.instance.SetMasterVolume((float)value);
	public void OnMusicVolumeChanged(double value) => SettingsManager.instance.SetMusicVolume((float)value);
	public void OnSFXVolumeChanged(double value) => SettingsManager.instance.SetSFXVolume((float)value);
	public void OnFullScreenToggled(bool value) => SettingsManager.instance.SetFullScreen(value);
	public void OnVSyncToggled(bool value) => SettingsManager.instance.SetVSync(value);
	public void OnShowFPSToggled(bool value) => SettingsManager.instance.SetShowFPS(value);

	public void OnResolutionSelected(long index) => SettingsManager.instance.SetResolution(resolution[index]);
	public void OnMaxFPSSelected(long index) => SettingsManager.instance.SetMaxFPS(fpsOptions[index]);

	public void OnBackPressed()
	{
		SettingsManager.instance.SaveSettings();
		if(isInGame &&pauseMenu != null)
		{
			Hide();
			pauseMenu.Show();
			pauseMenu.resumeButton.GrabFocus();
		}
		else
		{
			audio.Play();
			GetTree().CreateTimer(0.3f).Timeout += () =>
		{
			SceneManager.instance.GoBack();
		};
		}

	}
	public void OnApplyPressed()
	{
		audio.Play();
		SettingsManager.instance.SaveSettings();
		SettingsManager.instance.ApplySettings();
	}
}
