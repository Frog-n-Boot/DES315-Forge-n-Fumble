extends Node2D

var holdTimer = 0.0
var holdDuration = 1.0

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if Input.is_action_pressed("skip_tutorial"):
		holdTimer += delta
		if holdTimer >= holdDuration:
			get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")		
	else:
		holdTimer = 0.0
