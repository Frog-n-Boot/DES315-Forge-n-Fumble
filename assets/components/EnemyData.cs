using Godot;
using System;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string enemyName = "Enemy";
	[Export] public int maxHealth = 10;
	[Export] public int damage = 10;
	[Export] public int damageToPlayer = 10;
	[Export] public float speed = 5.0f;
	[Export] public Color color = new Color(1, 0, 0);
	[Export] public Vector3 scale = Vector3.One;
	[Export] public float lootDropChance = 0.5f;
	[Export] public Material enemyMat;
	[Export] public Mesh enemyMesh;
}
