using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
//using Math = ExMath;

public class SimonsMorse : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved;

	public KMSelectable[] LogKMS;
	public KMSelectable CampfireKMS;

	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModuleId = ModuleIdCounter++;
		//GetComponent<KMBombModule>().OnActivate += Activate;

		foreach (KMSelectable btn in LogKMS) {
			btn.OnInteract += delegate () { InputPress(btn); return false; };
			btn.OnInteractEnded += delegate () { InputRelease(btn); };
		}
	}

	void InputPress(KMSelectable btn) {	
		//btn.AddInteractionPunch();
		//Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);

		if(ModuleSolved) return;
		Debug.LogFormat("[Simon s'Morse #{0}] Bip!", ModuleId);

		/*
		int i = 0;
		for(; i < 3; i++){
			if(btn == InputKMS[i]) break;
		}
		*/
	}

	void InputRelease(KMSelectable btn) {
		if(ModuleSolved) return;

		/*
		int i = 0;
		for(; i < 3; i++){
			if(btn == InputKMS[i]) break;
		}
		*/
	}

	void Start () { //Shit that you calculate, usually a majority if not all of the module
		Debug.LogFormat("[Simon s'Morse #{0}] Up and running!", ModuleId);
	}

	void Solve () {
		ModuleSolved = true;
		GetComponent<KMBombModule>().HandlePass();
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
	}

	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} <TL/MR/BL> .-. to transmit the morse using the log at that position. !{0} campfire <tap/hold> to interact with the campfire.";
	#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpper();
		string[] Commands = Command.Split(' ');
		yield return null;

	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
	}

}
