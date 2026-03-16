/*
	lights less bright
	calc angle
	fire [red, white, green]
	input
		kill lights
		parse input
	colorblind
	twitch plays
	stages++
	sounds
	solve ani
		kill lights on solve
*/

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
	public GameObject[] LightOBJ;
	public Light[] Lights;

	private Dictionary<char, string> MorseDict = new Dictionary<char, string>() {
		{'0', "-----"},
		{'1', ".----"},
		{'2', "..---"},
		{'3', "...--"},
		{'4', "....-"},
		{'5', "....."},
		{'6', "-...."},
		{'7', "--..."},
		{'8', "---.."},
		{'9', "----."},
		{'A', ".-"},
		{'B', "-..."},
		{'C', "-.-."},
		{'D', "-.."},
		{'E', "."},
		{'F', ".-.."},
		{'G', "--."},
		{'H', "...."},
		{'I', ".."},
		{'J', ".---"},
		{'K', "-.-"},
		{'L', ".-.."},
		{'M', "--"},
		{'N', "-."},
		{'O', "---"},
		{'P', ".--."},
		{'Q', "--.-"},
		{'R', ".-."},
		{'S', "..."},
		{'T', "-"},
		{'U', "..-"},
		{'V', "...-"},
		{'W', ".--"},
		{'X', "-..-"},
		{'Y', "-.--"},
		{'Z', "--.."}
	};

	private Dictionary<string, Color32> ColorDict = new Dictionary<string, Color32>() {
		{"Red",     new Color32(255, 000, 000, 255)},
		{"Green",   new Color32(000, 255, 000, 255)},
		{"Blue",    new Color32(000, 000, 255, 255)},
		{"Cyan",    new Color32(000, 255, 255, 255)},
		{"Yellow",  new Color32(255, 255, 000, 255)},
		{"Magenta", new Color32(255, 000, 255, 255)},
		{"Orange",  new Color32(255, 127, 000, 255)},
		{"Pink",    new Color32(255, 127, 255, 255)},
		{"Black",   new Color32(000, 000, 000, 255)},
		{"White",   new Color32(255, 255, 255, 255)},
	};
	private Color32 BaseLogColor = new Color32(51, 28, 8, 255);

	private string[] LogColors = new string[6];
	private Coroutine TxCoroutine;

	private int Stage = 1;
	private int TxLog;
	private char TxChar;

	private int CurrentSeat;
	private int NextSeat;

	private char CampfireAngle;
	private char CurrentAngle;
	private char NextAngle;

	private bool isHeld = false;
	private float HeldTimer = 0f;
	private string InputMorse = "";
	private int InputLog;

	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;

		foreach (KMSelectable btn in LogKMS) {
			btn.OnInteract += delegate () { InputPress(btn); return false; };
			btn.OnInteractEnded += delegate () { InputRelease(btn); };
		}

		CampfireKMS.OnInteract += delegate () { FireDown(); return false; };
		CampfireKMS.OnInteractEnded += delegate () { FireUp(); };
	}

	void InputPress(KMSelectable btn) {	
		btn.AddInteractionPunch();
		//Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);

		if(ModuleSolved) return;
		StopTx();

		int i = 0; //N cw
		for(; i < 6; i++) if(btn == LogKMS[i]) break;

		HeldTimer = 0f;
		isHeld = true;
		InputLog = i;
	}

	void InputRelease(KMSelectable btn) {
		if(ModuleSolved) return;

		InputMorse += (HeldTimer > 0.3f) ? '-' : '.';
		
		HeldTimer = 0f;
		isHeld = false;
	}

	void FireDown() {

	}

	void FireUp() {

	}

	void Start () { //Shit that you calculate, usually a majority if not all of the module

		HashSet<string> chosenLogColors = new HashSet<string>();
		for(int i = 0; i < 6;){
			string newColor = ColorDict.ElementAt(Rnd.Range(0, ColorDict.Count)).Key;
			if(!chosenLogColors.Add(newColor)) continue;
			LogColors[i] = newColor;
			Lights[i].color = ColorDict[newColor];
			i++;
		}

		Debug.LogFormat("[Simon s'Morse #{0}] Colours of the logs (North clockwise): {1}", ModuleId, LogColors.Join(", "));

		TxLog = Rnd.Range(0,6);
		TxChar = MorseDict.Keys.ToArray()[Rnd.Range(0,36)];
		CurrentSeat = TxLog;

		for(int i = 0; i < 6; i++){
			LogKMS[i].GetComponent<MeshRenderer>().material.color = BaseLogColor;
			Lights[i].color = BaseLogColor;
		}

		CampfireAngle = Bomb.GetSerialNumber()[0];
		CurrentAngle = Bomb.GetSerialNumber()[1];

		Calc();

		Debug.LogFormat("[Simon s'Morse #{0}] Flashing log: {1}", ModuleId, TxLog);
		Debug.LogFormat("[Simon s'Morse #{0}] Flashing log color: {1}", ModuleId, LogColors[TxLog]);
		Debug.LogFormat("[Simon s'Morse #{0}] Next Log: {1}", ModuleId, NextSeat);

		Debug.LogFormat("[Simon s'Morse #{0}] Campfire's location: {1}", ModuleId, CampfireAngle);
		Debug.LogFormat("[Simon s'Morse #{0}] Transmitted character: {1}", ModuleId, TxChar);
		Debug.LogFormat("[Simon s'Morse #{0}] Current angle: {1}", ModuleId, CurrentAngle);
		Debug.LogFormat("[Simon s'Morse #{0}] Next angle: {1}", ModuleId, NextAngle);

		

	}

	void Activate() { //Lights on
		StartCoroutine(ColorLogs());

	}

	void Update() {
		if(isHeld){
			HeldTimer += Time.deltaTime;
			return;
		}

		if(InputMorse == "") return;

		if(HeldTimer < -1f){
			TxCoroutine = StartCoroutine(TxMorseOnLog(InputLog, InputMorse));
			
			Debug.LogFormat("[Simon s'Morse #{0}] Input: {1} on log {2}", ModuleId, MorseToChar(InputMorse), InputLog);
			InputMorse = "";
			HeldTimer = 0f;
		} else
			HeldTimer -= Time.deltaTime;

	}
	void Solve () {
		ModuleSolved = true;
		GetComponent<KMBombModule>().HandlePass();
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
	}

	void Calc() {
		int k = CurrentSeat;
		int[][] table = new int[][] {
			new int[] {k+4, 0,   1,   3,   k,   k+3, 4,   k+5, k,   k+2 }, //TR
			new int[] {k,   k+1, 0,   k+2, 2,   4,   k+1, k+3, k+5, 1   }, //MR
			new int[] {k+3, 5,   k+5, 4,   3,   k+4, k,   k+4, 5,   2   }, //BR
			new int[] {3,   k+2, k+4, 5,   k+4, 0,   k+2, 1,   k+1, 0   }, //BL
			new int[] {k+1, 2,   3,   1,   4,   2,   k+5, 5,   k+3, 4   }, //ML
			new int[] {5,   k+5, k+2, k+3, 3,   k+1, 1,   0,   2,   k   }  //TL
		};

		NextSeat = table[TxLog][ColorDict.Keys.ToList().IndexOf(LogColors[TxLog])] % 6;

		Vector2Int offset = GetPosOfChar(TxChar) - GetPosOfChar(CampfireAngle) + new Vector2Int(6,6);
		Vector2Int newAnglePos = offset + GetPosOfChar(CurrentAngle);

		NextAngle = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(newAnglePos.x % 6) + (newAnglePos.y % 6) * 6];

	}

	char MorseToChar(string morse){
		if(!MorseDict.ContainsValue(InputMorse)) return '?';
		return MorseDict.Where(p => p.Value == morse).Select(p => p.Key).ToArray()[0];
	}

	Vector2Int GetPosOfChar (char x) {
		string b36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		int i = b36.IndexOf(x);

		return new Vector2Int(i%6, i/6);
	}

	IEnumerator TxMorseOnLog(int log, string currentChar) {
		foreach(GameObject logbulb in LightOBJ) logbulb.SetActive(false);
		int i = 0;

		while(true){
			LightOBJ[log].SetActive(false);
			yield return new WaitForSeconds(i == currentChar.Length ? 1.0f : 0.15f);
		
			i %= currentChar.Length;

			LightOBJ[log].SetActive(true);
			yield return new WaitForSeconds(currentChar[i++] == '.' ? 0.15f : 0.6f);
		}
	}

	void StopTx(){
		if(TxCoroutine != null) StopCoroutine(TxCoroutine);
		foreach(GameObject logbulb in LightOBJ) logbulb.SetActive(false);
	}
	
	IEnumerator ColorLogs() {
		double t = 0.01f;
		yield return new WaitForSeconds(1.5f);

		for(int i = 0; i < 6; i++){
			
			if(LogColors[i] == "Black"){
				Lights[i].color = ColorDict["White"];			
			} else
				Lights[i].color = ColorDict[LogColors[i]];

			while (t < 0.99f) {
				LogKMS[i].GetComponent<MeshRenderer>().material.color =  Color32.Lerp(BaseLogColor, ColorDict[LogColors[i]], (float)t); 
				t = Math.Pow(t, 0.76f);
				yield return new WaitForSeconds(0.03f);
			}

			t = 0.01f;
		}

		yield return new WaitForSeconds(0.5f);
		TxCoroutine = StartCoroutine(TxMorseOnLog(TxLog, MorseDict[TxChar]));
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
