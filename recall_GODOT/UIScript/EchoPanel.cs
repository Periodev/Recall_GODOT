using Godot;
using System;
using System.Collections.Generic;
using CombatCore;

public partial class EchoPanel : Control
{
	[Export] public Combat CombatCtrl;

	// UI 組件 - 手動在 Inspector 綁定
	[Export] private Button BtnEchoSlot0;
	[Export] private Button BtnEchoSlot1;
	[Export] private Button BtnEchoSlot2;
	[Export] private Button BtnEchoSlot3;
	[Export] private Button BtnEchoSlot4;
	[Export] private Label EchoName;
	[Export] private Label Recipe;
	[Export] private RichTextLabel Summary;
	[Export] private Button BtnPlay;
	[Export] private Button BtnCancel;
	[Export] private Label Reason;




}
