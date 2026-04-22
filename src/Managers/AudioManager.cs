using Godot;
using System;

public partial class AudioManager : Node
{
	public static AudioManager instance {get; private set;}

	private AudioStreamPlayer musicPlayer;
	private Tween tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		instance = this;
		musicPlayer = new AudioStreamPlayer();
		musicPlayer.Bus = "Music";
		AddChild(musicPlayer);
	}

	public void PlayMusic(AudioStream stream, float fadeOutDuration = 1.5f, float fadeInDuration = 1.1f)
	{
		if (!musicPlayer.Playing)
		{
			musicPlayer.Stream = stream;
			musicPlayer.VolumeDb = -40f;
			musicPlayer.Play();
			return;
		}

		tween?.Kill();
		tween = CreateTween();

		tween.TweenProperty(musicPlayer, "volume_db", -80, fadeOutDuration);

		tween.TweenCallback(Callable.From(() =>
		{
			musicPlayer.Stream = stream;
			musicPlayer.Play();
		}));

		tween.TweenProperty(musicPlayer, "volume_db", -30, fadeInDuration);
	}

	public void StopMusic(float fadeOutDuration = 1.5f)
	{
		tween?.Kill();
		tween = CreateTween();
		tween.TweenProperty(musicPlayer, "volume_db", -80, fadeOutDuration);
		tween.TweenCallback(Callable.From(() => musicPlayer.Stop()));
	}
}
