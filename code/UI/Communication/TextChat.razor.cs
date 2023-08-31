﻿using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Pace.UI;

public partial class TextChat
{
	public static TextChat Instance { get; private set; }

	public bool IsOpen
	{
		get => HasClass( "open" );
		set
		{
			SetClass( "open", value );
			if ( value )
			{
				Input.Focus();
				Input.Text = string.Empty;
				Input.Label.SetCaretPosition( 0 );
			}
		}
	}

	private const int MaxItems = 100;
	private const float MessageLifetime = 10f;

	private Panel Canvas { get; set; }
	private TextEntry Input { get; set; }

	private readonly Queue<TextChatEntry> _entries = new();

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );

		Canvas.PreferScrollToBottom = true;
		Input.AcceptsFocus = true;
		Input.AllowEmojiReplace = true;

		Instance = this;
	}

	public override void Tick()
	{
		if ( Sandbox.Input.Pressed( InputAction.Chat ) )
			Open();
	}

	private void AddEntry( TextChatEntry entry )
	{
		Canvas.AddChild( entry );
		Canvas.TryScrollToBottom();

		entry.BindClass( "stale", () => entry.Lifetime > MessageLifetime );

		_entries.Enqueue( entry );
		if ( _entries.Count > MaxItems )
			_entries.Dequeue().Delete();
	}

	private void Open()
	{
		AddClass( "open" );
		Input.Focus();
		Canvas.TryScrollToBottom();
	}

	private void Close()
	{
		RemoveClass( "open" );
		Input.Blur();
		Input.Text = string.Empty;
		Input.Label.SetCaretPosition( 0 );
	}

	private void Submit()
	{
		var message = Input.Text.Trim();
		Input.Text = "";

		Close();

		if ( string.IsNullOrWhiteSpace( message ) )
			return;

		/*		if ( message == "!rtv" && Game.LocalClient.HasRockedTheVote() )
				{
					AddInfoEntry( "You have already rocked the vote!" );
					return;
				}
		*/
		SendChat( message );
	}

	[ConCmd.Server( "ttt_say" )]
	public static void SendChat( string message )
	{
		var caller = ConsoleSystem.Caller;

		/*		if ( message == "!rtv" )
				{
					GameManager.RockTheVote();
					return;
				}*/


		AddChatEntry( To.Everyone, caller.SteamId, caller.Name, message );
	}

	[ClientRpc]
	public static void AddChatEntry( long playerId, string playerName, string message )
	{
		Instance?.AddEntry( new TextChatEntry( playerId, playerName, message ) );
	}

	[ClientRpc]
	public static void AddInfoEntry( string message )
	{
		Instance?.AddEntry( new TextChatEntry( message ) );
	}
}

public partial class TextEntry : Sandbox.UI.TextEntry
{
	public event Action OnTabPressed;

	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( e.Button == "tab" )
		{
			OnTabPressed?.Invoke();
			return;
		}

		base.OnButtonTyped( e );
	}
}
