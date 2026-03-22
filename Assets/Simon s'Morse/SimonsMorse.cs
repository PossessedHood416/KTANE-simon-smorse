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
	public KMSelectable SmorseMod;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved;

	public KMSelectable[] LogKMS;
	public KMSelectable CampfireKMS;
	public GameObject[] LightOBJ;
	public Light[] Lights;
	public TextMesh[] ColorblindTxt;
	public Light CampfireLight;
	public ParticleSystem CampfireParticle;

	public KMColorblindMode colorblindScript;
	private SimonsMorseSettings Settings = new SimonsMorseSettings();

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
		{'F', "..-."},
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

	private Color32 BaseLogColor = new Color32(51, 28, 8, 255);
	private Dictionary<string, Color32> ColorDict = new Dictionary<string, Color32>() {
		{"Red",			new Color32(255, 000, 000, 255)},
		{"Green",		new Color32(000, 255, 000, 255)},
		{"Blue",		new Color32(000, 000, 255, 255)},
		{"Cyan",		new Color32(000, 255, 255, 255)},
		{"Yellow",		new Color32(255, 255, 000, 255)},
		{"Magenta", 	new Color32(255, 000, 255, 255)},
		{"Orange",		new Color32(255, 127, 000, 255)},
		{"Pink",		new Color32(255, 127, 255, 255)},
		{"Black",		new Color32(050, 050, 050, 255)},
		{"White",		new Color32(255, 255, 255, 255)},
	};
	private Dictionary<string, string> ColorblindDict = new Dictionary<string, string>() {
		{"Red",		 "R"},
		{"Green",	 "G"},
		{"Blue",		"B"},
		{"Cyan",		"C"},
		{"Yellow",	"Y"},
		{"Magenta", "M"},
		{"Orange",	"O"},
		{"Pink",		"I"},
		{"Black",	 "K"},
		{"White",	 "W"},
	};

	private Dictionary<int, string> LogPosDict = new Dictionary<int, string>() {
		{0, "TR"},
		{1, "MR"},
		{2, "BR"},
		{3, "BL"},
		{4, "ML"},
		{5, "TL"},
	};

	private Color32[] CampfireColors = new Color32[] {
		new Color32(255, 142, 000, 255),
		new Color32(255, 255, 255, 255),
		new Color32(000, 255, 000, 255),
	};

	private string[] LogColors = new string[6];
	private Coroutine TxCoroutine;
	private Coroutine CampfireCoroutine;

	private int Stage = 1;
	private string ModState = "ANI";
	private int[] TxLogs = new int[4];
	private char[] TxChars = new char[4];

	private int CurrentSeat;
	private int NextSeat;

	private char CampfireAngle;
	private char CurrentAngle;
	private char NextAngle;

	private bool isLogHeld = false;
	private float LogHeldTimer = 0f;
	private string InputMorse = "";
	private int InputLog = -1;

	private bool isFireHeld = false;
	private float FireHeldTimer = 0f;

	private KMAudio.KMAudioRef BgNoise = null;
	private KMAudio.KMAudioRef MorseSound = null;
	private bool isFocused = false;

	//mod setup
	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModConfig<SimonsMorseSettings> modConfig = new ModConfig<SimonsMorseSettings>("SimonsMorseSettings");
		Settings = modConfig.Settings;
		modConfig.Settings = Settings;

		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;

		foreach (KMSelectable btn in LogKMS) {
			btn.OnInteract += delegate () { InputPress(btn); return false; };
			btn.OnInteractEnded += delegate () { InputRelease(btn); };
		}

		CampfireKMS.OnInteract += delegate () { FireDown(); return false; };
		CampfireKMS.OnInteractEnded += delegate () { FireUp(); };

		SmorseMod.OnFocus += delegate { PlayBgNoise(); isFocused = true; };
		SmorseMod.OnDefocus += delegate { StopBgNoise(); isFocused = false; };
	}

	void Start () { //Shit that you calculate, usually a majority if not all of the module
		CampfireLight.spotAngle = 0f;

		HashSet<string> chosenLogColors = new HashSet<string>();
		for(int i = 0; i < 6;){
			string newColor = ColorDict.ElementAt(Rnd.Range(0, ColorDict.Count)).Key;
			if(!chosenLogColors.Add(newColor)) continue;
			LogColors[i] = newColor;
			Lights[i].color = ColorDict[newColor];
			i++;
		}

		for(int i = 0; i < 6; i++){
			LogKMS[i].GetComponent<MeshRenderer>().material.color = BaseLogColor;
			Lights[i].color = BaseLogColor;
			ColorblindTxt[i].text = "";
		}
		
		CampfireAngle = Bomb.GetSerialNumber()[0];
		CurrentAngle = Bomb.GetSerialNumber()[1];
		
		Debug.LogFormat("[Simon s'Morse #{0}] Colours of the logs (North clockwise): {1}", ModuleId, LogColors.Join(", "));
		Debug.LogFormat("[Simon s'Morse #{0}] Campfire's location character: {1}", ModuleId, CampfireAngle);

		Calc();
	}

	void Activate() { //Lights on
		StartCoroutine(ColorLogs());
		StartCoroutine(CampfireFlickerAni());
		StartCampfireRecolour(0);
	}


	//mod inputs / general
	void InputPress(KMSelectable btn) {	
		btn.AddInteractionPunch();
		//Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);

		if(ModuleSolved || (ModState != "READ" && ModState != "WRITE")) return;

		int i = 0; //N cw
		for(; i < 6; i++) if(btn == LogKMS[i]) break;

		if(InputLog != -1 && InputLog != i) return;

		StopTx();
		ModState = "WRITE";

		LogHeldTimer = 0f;
		isLogHeld = true;
		InputLog = i;
	}

	void InputRelease(KMSelectable btn) {
		if(ModuleSolved || (ModState != "READ" && ModState != "WRITE") || !isLogHeld) return;

		InputMorse += (LogHeldTimer > 0.3f) ? '-' : '.';
		
		LogHeldTimer = 0f;
		isLogHeld = false;
	}

	void FireDown() {
		if(ModuleSolved) return;
		isFireHeld = true;
	}

	void FireUp() {
		isFireHeld = false;
		if(ModuleSolved || ModState != "READWRITE" || FireHeldTimer > 2f) return;

		Debug.LogFormat("[Simon s'Morse #{0}] Input: ({3}) {1} on log {2}", ModuleId, MorseToChar(InputMorse), LogPosDict[InputLog], InputMorse);
		if(NextSeat == InputLog && NextAngle == MorseToChar(InputMorse)) StageProgress();
		else Strike();
	}

	void Update() {
		if(ModuleSolved || ModState == "ANI") return;

		if(ModState == "READWRITE"){
			if(isFireHeld) FireHeldTimer += Time.deltaTime;
			if(FireHeldTimer > 2f) SoftReset();
			return;
		}

		if(isLogHeld){
			LogHeldTimer += Time.deltaTime;
			return;
		}

		if(InputMorse == "") return;

		if(LogHeldTimer < -1f){
			TxCoroutine = StartCoroutine(TxMorseOnLog(InputLog, InputMorse));
			LogHeldTimer = 0f;
			ModState = "READWRITE";
			StartCampfireRecolour(1);
		} else
			LogHeldTimer -= Time.deltaTime;
	}

	void Solve () {
		Debug.LogFormat("[Simon s'Morse #{0}] Module solved.", ModuleId);
		ModuleSolved = true;
		ModState = "SOLVED";

		Audio.PlaySoundAtTransform("Reset", CampfireKMS.transform);
		StopTx();
		StartCampfireRecolour(2);
		StartCoroutine(UncolorLogs());
		GetComponent<KMBombModule>().HandlePass();
	}

	void Strike () {
		Debug.LogFormat("[Simon s'Morse #{0}] Strike!", ModuleId);
		GetComponent<KMBombModule>().HandleStrike();
		SoftReset();
	}

	void StageProgress() {
		Stage++;
		
		if(Stage >= 5){
			Solve();
			return;
		}

		CurrentSeat = NextSeat;
		CurrentAngle = NextAngle;

		SoftReset();
		Calc();
	}

	void SoftReset() {
		Audio.PlaySoundAtTransform("Reset", CampfireKMS.transform);
		InputMorse = "";
		InputLog = -1;

		FireHeldTimer = 0f;
		LogHeldTimer = 0f;
		
		StartCampfireRecolour(0);
		
		StopTx();
		TxCoroutine = StartCoroutine(TxMorseOnLog());
		
		ModState = "READ";
	}


	//calculations
	void Calc() {
		TxLogs[Stage-1] = Rnd.Range(0,6);
		TxChars[Stage-1] = MorseDict.Keys.ToArray()[Rnd.Range(0,36)];
		
		if(Stage == 1) CurrentSeat = TxLogs[0];
		
		int k = CurrentSeat;
		int[][] table = new int[][] {
			new int[] {k+4, 0,	 1,	 3,	 k,	 k+3, 4,	 k+5, k,	 k+2 }, //TR
			new int[] {k,	 k+1, 0,	 k+2, 2,	 4,	 k+1, k+3, k+5, 1	 }, //MR
			new int[] {k+3, 5,	 k+5, 4,	 3,	 k+4, k,	 k+4, 5,	 2	 }, //BR
			new int[] {3,	 k+2, k+4, 5,	 k+4, 0,	 k+2, 1,	 k+1, 0	 }, //BL
			new int[] {k+1, 2,	 3,	 1,	 4,	 2,	 k+5, 5,	 k+3, 4	 }, //ML
			new int[] {5,	 k+5, k+2, k+3, 3,	 k+1, 1,	 0,	 2,	 k	 }	//TL
		};

		NextSeat = table[TxLogs[Stage-1]][ColorDict.Keys.ToList().IndexOf(LogColors[TxLogs[Stage-1]])] % 6;

		Vector2Int offset = GetPosOfChar(TxChars[Stage-1]) - GetPosOfChar(CampfireAngle) + new Vector2Int(6,6);
		Vector2Int newAnglePos = offset + GetPosOfChar(CurrentAngle);

		NextAngle = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(newAnglePos.x % 6) + (newAnglePos.y % 6) * 6];

		Debug.LogFormat("[Simon s'Morse #{0}] ===========STAGE {1}===========", ModuleId, Stage);

		Debug.LogFormat("[Simon s'Morse #{0}] Flashing log: {1}", ModuleId, LogPosDict[TxLogs[Stage-1]]);
		Debug.LogFormat("[Simon s'Morse #{0}] Flashing log color: {1}", ModuleId, LogColors[TxLogs[Stage-1]]);
		Debug.LogFormat("[Simon s'Morse #{0}] Flashing morse character: {1}", ModuleId, TxChars[Stage-1]);


		Debug.LogFormat("[Simon s'Morse #{0}] Current seat: {1}", ModuleId, LogPosDict[CurrentSeat]);
		Debug.LogFormat("[Simon s'Morse #{0}] Current angle: {1}", ModuleId, CurrentAngle);
		
		Debug.LogFormat("[Simon s'Morse #{0}] Next log: {1}", ModuleId, LogPosDict[NextSeat]);
		Debug.LogFormat("[Simon s'Morse #{0}] Next angle: {1}", ModuleId, NextAngle);
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


	//anis and coroutines
	IEnumerator TxMorseOnLog(int log, string currentChar) {
		foreach(GameObject logbulb in LightOBJ) logbulb.SetActive(false);
		int i = 0;

		while(true){
			LightOBJ[log].SetActive(false);
			if(MorseSound != null) MorseSound.StopSound();
			yield return new WaitForSeconds(i == currentChar.Length ? 1.6f : 0.1f);
		
			i %= currentChar.Length;

			LightOBJ[log].SetActive(true);
			if(isFocused) MorseSound = Audio.PlaySoundAtTransformWithRef(LogColors[log], CampfireKMS.transform);
			yield return new WaitForSeconds(currentChar[i++] == '.' ? 0.1f : 0.6f);
		}
	}

	IEnumerator TxMorseOnLog() {
		//evil but it works		
		int stageI = 0;
		int msI = 0;
		int currLog = TxLogs[0];
		string currTx = MorseDict[TxChars[0]];

		foreach(GameObject logbulb in LightOBJ) logbulb.SetActive(false);
		yield return new WaitForSeconds(0.5f);

		while(true){
			while(msI < currTx.Length){
				LightOBJ[currLog].SetActive(true);
				if(isFocused) MorseSound = Audio.PlaySoundAtTransformWithRef(LogColors[currLog], CampfireKMS.transform);
				yield return new WaitForSeconds(currTx[msI++] == '.' ? 0.1f : 0.6f);

				LightOBJ[currLog].SetActive(false);
				if(MorseSound != null) MorseSound.StopSound();

				yield return new WaitForSeconds(0.1f);
			}

			stageI = (stageI+1) % (Stage);
			msI = 0;
			currLog = TxLogs[stageI];
			currTx = MorseDict[TxChars[stageI]];

			yield return new WaitForSeconds(stageI != 0 ? 0.50f : 1.50f);
		}
	}

	void StopTx(){
		if(TxCoroutine != null) StopCoroutine(TxCoroutine);
		foreach(GameObject logbulb in LightOBJ) logbulb.SetActive(false);
		if(MorseSound != null) MorseSound.StopSound();
	}

	void StartCampfireRecolour(int i) {
		if(CampfireCoroutine != null) StopCoroutine(CampfireCoroutine);
		CampfireCoroutine = StartCoroutine(RecolorCampfire(i));
	}
	
	IEnumerator ColorLogs() {
		double t = 0.01f;
		bool cb = colorblindScript.ColorblindModeActive;
		yield return new WaitForSeconds(1.5f);

		for(int i = 0; i < 6; i++){

			if(LogColors[i] == "Black"){
				Lights[i].color = ColorDict["White"];			
			} else
				Lights[i].color = ColorDict[LogColors[i]];

			while (t < 0.99f) {
				LogKMS[i].GetComponent<MeshRenderer>().material.color =	Color32.Lerp(BaseLogColor, ColorDict[LogColors[i]], (float)t); 
				t = Math.Pow(t, 0.76f);
				yield return new WaitForSeconds(0.03f);
			}

			if(cb) ColorblindTxt[i].text = ColorblindDict[LogColors[i]];
			t = 0.01f;
		}

		yield return new WaitForSeconds(0.5f);
		TxCoroutine = StartCoroutine(TxMorseOnLog());
		ModState = "READ";
	}

	IEnumerator UncolorLogs() {
		double t = 0.01f;
		
		yield return new WaitForSeconds(1f);

		for(int i = 5; i >= 0; i--){
			Color32 fro = LogKMS[i].GetComponent<MeshRenderer>().material.color;
			ColorblindTxt[i].text = "";

			while (t < 0.99f) {
				LogKMS[i].GetComponent<MeshRenderer>().material.color =	Color32.Lerp(fro, BaseLogColor, (float)t); 
				t = Math.Pow(t, 0.76f);
				yield return new WaitForSeconds(0.03f);
			}

			t = 0.01f;
		}

		yield return new WaitForSeconds(0.5f);
	}

	IEnumerator CampfireFlickerAni() {
		float theta = CampfireLight.spotAngle;
		yield return null;
		
		while(!ModuleSolved){
			if(theta < 40f) theta += 0.5f;
			else
			if(theta > 80f) theta -= 0.5f;
			else
			theta += Rnd.Range(0,2) == 1 ? 0.5f : -0.5f;

			CampfireLight.spotAngle = theta;
			yield return new WaitForSeconds(0.015f);
		}
	}

	IEnumerator RecolorCampfire(int i) {
		double t = 0.01f;
		yield return null;

		Color32 fro = CampfireLight.color;
		Color32 to = CampfireColors[i];

		while (t < 0.99f) {
			Color32 newcol = Color32.Lerp(fro, to, (float)t);
			CampfireLight.color = newcol; 
			CampfireParticle.startColor = newcol;
			t = Math.Pow(t, 0.5f);
			yield return new WaitForSeconds(0.03f);
		}
	}


	//sounds
	void PlayBgNoise() {
		if(!Settings.PlayAmbience) return;
		BgNoise = Audio.PlaySoundAtTransformWithRef("Background", CampfireKMS.transform);
	}

	void StopBgNoise() {
		if(BgNoise == null) return;
		BgNoise.StopSound();
	}


	//tp
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} <TL/MR/BL> .-. to transmit the morse using the log at that position. !{0} reset, !{0} submit, !{0} colo(u)rblind/cb.";
	#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpper();
		string[] Commands = Command.Split(' ');
		yield return null;

		if(Regex.IsMatch(Commands[0], @"^[TMB][LR]$")){
			if(Commands.Length != 2){
				yield return "sendtochaterror Please provide some morse, expecting 2 arguments.";
				yield break;
			}
			if(!Regex.IsMatch(Commands[1], @"^(\.|-)+$")){
				yield return "sendtochaterror Invalid morse: " + Commands[1];
				yield break;
			}
			if(ModState != "READ"){
				yield return "sendtochaterror The module isn't accepting morse at this time.";
				yield break;
			}

			StartCoroutine(TwitchInputOnLog(LogPosDict.Where(p => p.Value == Commands[0]).Select(p => p.Key).ToArray()[0], Commands[1]));
			yield break;
		} else if(Commands[0] == "SUBMIT"){
			if(ModState != "READWRITE"){
				yield return "sendtochaterror The module cannot be submitted at this time.";
				yield break;
			}
			StartCoroutine(TwitchHoldFire(0.1f));

		} else if(Commands[0] == "RESET"){
			if(ModState != "READWRITE"){
				yield return "sendtochaterror The module cannot be reset at this time.";
				yield break;
			}
			StartCoroutine(TwitchHoldFire(2.5f));

		} else if (Regex.IsMatch(Commands[0], @"^C(B|OLO(|U)RBLIND)$")){
			bool cb = ColorblindTxt[0].text != "";
			for(int i = 0; i < 6; i++){
				ColorblindTxt[i].text = cb ? "" : ColorblindDict[LogColors[i]];
			}
		
		} else {
			yield return "sendtochaterror Invalid command: " + Commands[0];
			yield break;
		}
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
		while(ModState == "WRITE") yield return new WaitForSeconds(0.2f);
		while(ModState != "READ"){
			StartCoroutine(TwitchHoldFire(2.5f));
			yield return new WaitForSeconds(2.6f);
		}

		while(!ModuleSolved){
			StartCoroutine(TwitchInputOnLog(NextSeat, MorseDict[NextAngle]));
			yield return new WaitForSeconds(MorseDict[NextAngle].Length * 0.7f + 1.2f);

			StartCoroutine(TwitchHoldFire(0.2f));
			yield return new WaitForSeconds(0.3f);
		}

	}

	IEnumerator TwitchInputOnLog(int log, string morse){
		yield return null;
		for(int i = 0; i < morse.Length; i++){
			InputPress(LogKMS[log]);
			yield return new WaitForSeconds(morse[i] == '-' ? 0.5f : 0.1f);

			InputRelease(LogKMS[log]);
			yield return new WaitForSeconds(0.2f);
		}
	}

	IEnumerator TwitchHoldFire(float time){
		yield return null;
		FireDown();
		yield return new WaitForSeconds(time);
		FireUp();
	}


	//mod settings
	class SimonsMorseSettings {
		public bool PlayAmbience = true;
	}

	static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[] {
		new Dictionary<string, object> {
			 { "Filename", "SimonsMorse.json" },
			 { "Name", "SimonsMorse Settings" },
			 { "Listing", new List<Dictionary<string, object>>{
					new Dictionary<string, object> {
						 { "Key", "Play ambience" },
						 { "Text", "The module will play ambience when it is focused." }
					}
			 } }
		}
	 };
}