﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EQ2.ISXEQ2;
using LavishVMAPI;
using LavishScriptAPI;
using System.Threading;
using System.IO;
using System.Speech.Synthesis;

namespace EQ2GlassCannon
{
	/// Basically the entire bot is run out of here, so that only a static PlayerController can access the values
	/// in the dynamically allocated PlayerControllers, and vice versa.
	/// Outside classes are not allowed to peek inside.
	public partial class PlayerController
	{
		private static Extension s_Extension = null;
		private static EQ2.ISXEQ2.ISXEQ2 s_ISXEQ2 = null;
		private static EQ2.ISXEQ2.EQ2 s_EQ2 = null;
		private static EQ2Event s_eq2event = null;
		private static SpeechSynthesizer s_SpeechSynthesizer = null;

		private static Character s_Me = null;
		protected static Character Me { get { return s_Me; } }

		private static Actor s_MeActor = null;
		protected static Actor MeActor { get { return s_MeActor; } }

		private static string s_strName = string.Empty;
		/// <summary>
		/// The current player's own name. Cached during UpdateStaticGlobals().
		/// </summary>
		protected static string Name { get { return s_strName; } }

		private static int s_iAbilityCount = 0;
		protected static int AbilityCount { get { return s_iAbilityCount; } }

		private static bool s_bIsInRaid = false;
		protected static bool IsInRaid { get { return s_bIsInRaid; } }

		private static bool s_bIsInGroup = false;
		protected static bool IsInGroup { get { return s_bIsInGroup; } }

		protected static string ServerName { get { return s_EQ2.ServerName; } }

		private static long s_lFrameCount = 0;
		protected static long FrameCount { get { return s_lFrameCount; } }

		private static PlayerController s_Controller = null;
		protected static bool s_bContinueBot = true;
		protected static bool s_bRefreshKnowledgeBook = false;
		protected static string s_strCurrentINIFilePath = string.Empty;
		protected static string s_strSharedOverridesINIFilePath = string.Empty;
		private static string s_strNewWindowTitle = null;
		private static SetCollection<string> s_PressedKeys = new SetCollection<string>();
		private static Dictionary<string, DateTime> m_RecentThrottledCommandIndex = new Dictionary<string, DateTime>();
		private static SetCollection<string> m_RegisteredCustomSlashCommands = new SetCollection<string>();
		private readonly static LavishScriptAPI.Delegates.CommandTarget s_CustomSlashCommandDelegate = OnLavishScriptCommand;

		/************************************************************************************/
		protected static bool UpdateStaticGlobals()
		{
			/// These become invalid from frame to frame.
			/// I find that a LOT of Character members throw exceptions.
			/// I may need to cache them here the same way I cache abilities.
			try
			{
				s_MeActor = s_Me.ToActor();
				s_strName = Me.Name;
				s_iAbilityCount = Me.NumAbilities;
				s_bIsInRaid = Me.InRaid;
				s_bIsInGroup = Me.Grouped;
			}
			catch
			{
				Program.Log("Exception thrown when updating global variable aliases for the frame lock.");
				return false;
			}

			return true;
		}

		/************************************************************************************/
		public static bool Initialize()
		{
			Program.Log("Waiting for ISXEQ2 to initialize...");

			using (new FrameLock(true))
			{
				/// Just use this for debugging; otherwise it crashes the client when the bot terminates.
				//LavishScript.RequireExplicitFrameLock = true;

				s_Extension = new Extension();

				/// Patch notes say these are now persistant.
				s_ISXEQ2 = s_Extension.ISXEQ2();
				s_EQ2 = s_Extension.EQ2();
				s_Me = s_Extension.Me();

				LavishScript.Events.AttachEventTarget(LavishScript.Events.RegisterEvent("OnFrame"), OnFrame_EventHandler);
			}

			while (true)
			{
				using (new FrameLock(true))
				{
					if (s_ISXEQ2.IsReady)
						break;
				}
			}

			using (new FrameLock(true))
			{
				s_ISXEQ2.SetActorEventsRange(50.0f);

				s_eq2event = new EQ2Event();
				s_eq2event.ChoiceWindowAppeared += new EventHandler<LSEventArgs>(OnChoiceWindowAppeared_EventHandler);
				s_eq2event.RewardWindowAppeared += new EventHandler<LSEventArgs>(OnRewardWindowAppeared_EventHandler);
				s_eq2event.IncomingChatText += new EventHandler<LSEventArgs>(OnIncomingChatText_EventHandler);
				s_eq2event.IncomingText += new EventHandler<LSEventArgs>(OnIncomingText_EventHandler);

				RegisterCustomSlashCommands(
					"gc_assist",
					"gc_attack",
					"gc_attackassist",
					"gc_cancelgroupbuffs",
					"gc_changesetting",
					"gc_debug",
					"gc_exit",
					"gc_exitprocess",
					"gc_findactor",
					"gc_openini",
					"gc_openoverridesini",
					"gc_reloadsettings",
					"gc_stance",
					"gc_spawnwatch",
					"gc_target",
					"gc_tts",
					"gc_version",
					"gc_withdraw");
			}

			return true;
		}

		/************************************************************************************/
		public static bool TestSpeed()
		{
			double fTestTime = 2.0;

			/// This giant code chunk was a huge necessity due to the completely random lag that happens
			/// inside the UpdateGlobals() function. A laggy launch will never free up and a free launch
			/// will never get laggy.  Thus we veto laggy launches and tell the user to re-launch.
			Program.Log("Testing the speed of root object lookup. Please wait {0:0.0} seconds...", fTestTime);
			int iFramesElapsed = 0;
			int iBadFrames = 0;
			double fTotalTimes = 0.0;
			DateTime SlowFrameTestStartTime = DateTime.Now;
			while (DateTime.Now - SlowFrameTestStartTime < TimeSpan.FromSeconds(fTestTime))
			{
				iFramesElapsed++;
				Frame.Wait(true);
				try
				{
					DateTime BeforeTime = DateTime.Now;
					PlayerController.UpdateStaticGlobals(); /// This is the line we're testing.
					DateTime AfterTime = DateTime.Now;

					double fElapsedTime = (AfterTime - BeforeTime).TotalMilliseconds;
					fTotalTimes += fElapsedTime;
					//Program.Log("{0} : {1:0.0}", iFramesElapsed, fElapsedTime);

					/// A correctly functioning access to the root ISXEQ2 globals should take 0-1 milliseconds.
					/// Anything higher is unacceptable and demonstrably wrong.
					if (fElapsedTime > 4.0)
						iBadFrames++;
				}
				finally
				{
					Frame.Unlock();
				}
			}
			double fFramesPerSecond = (double)iFramesElapsed / (DateTime.Now - SlowFrameTestStartTime).TotalSeconds;
			double fAverageTime = fTotalTimes / (double)iFramesElapsed;
			double fBadFramePercentage = (double)iBadFrames / (double)iFramesElapsed * 100;
			Program.Log("Average time per object lookup was {0:0} ms, with {1:0.0}% of frames ({2} / {3}) lagging out ({4:0.0} FPS).", fAverageTime, fBadFramePercentage, iBadFrames, iFramesElapsed, fFramesPerSecond);
#if DEBUG
			if (fFramesPerSecond < 10)
#else
			if (fAverageTime > 2 || fBadFramePercentage > 10)
#endif
			{
				Program.Log("Aborting due to substantial ISXEQ2 lag. Please restart EQ2GlassCannon.");
				return false;
			}

			return true;
		}

		/************************************************************************************/
		public static void Run()
		{
			s_strSharedOverridesINIFilePath = Path.Combine(Program.ConfigurationFolderPath, "SharedOverrides.ini");
			if (!File.Exists(s_strSharedOverridesINIFilePath))
				File.Create(s_strSharedOverridesINIFilePath).Close();

			string strLastClass = string.Empty;
			bool bFirstZoningFrame = true;

			do
			{
				Frame.Wait(true);
				try
				{
					/// Call the controller if we zone. During zoning, no game variables are assumed to be worth a damn.
					if (s_EQ2.GetMember<string>("Zoning") != "0")
					{
						if (bFirstZoningFrame)
						{
							Program.Log("Zoning...");
							bFirstZoningFrame = false;

							ReleaseAllKeys();
							m_RecentThrottledCommandIndex.Clear();
							if (s_Controller != null)
								s_Controller.OnZoningBegin();

							Program.RunGarbageCollector();
						}
						continue;
					}
					else
					{
						if (!UpdateStaticGlobals())
							continue;

						if (!bFirstZoningFrame)
						{
							Program.Log("Done zoning.");
							bFirstZoningFrame = true;

							if (s_Controller != null)
								s_Controller.OnZoningComplete();

							ApplyGameSettings();
						}
					}

					try
					{
						/// An amazing little thing I discovered while watching console spam.
						/// EQ2 will prefix your character name if you are watching a flythrough zone intro video.
						/// Pressing Escape kills it.
						if (Me.Group(0).Name.StartsWith("Flythrough_"))
						{
							Program.Log("Zone flythrough sequence detected, attempting to cancel with the Esc key...");
							LavishScript.ExecuteCommand("press esc");
							continue;
						}
					}
					catch
					{
					}


					/// Yay for lazy!
					if (s_Controller != null && !string.IsNullOrEmpty(s_EQ2.PendingQuestName) && s_EQ2.PendingQuestName != "None")
					{
						Program.Log("Quest offered: \"{0}\".", s_EQ2.PendingQuestName);

						if (s_Controller.m_ePositioningStance == PlayerController.PositioningStance.DoNothing)
							Program.Log("Character is in do-nothing stance; ignoring offered quest.");
						else
						{
							/// Stolen from "EQ2Quest.iss". The question I have: isn't AcceptPendingQuest() redundant then?
							/// eq2ui_popup_rewardpack.xml
							EQ2UIPage QuestPage = s_Extension.EQ2UIPage("Popup", "RewardPack");
							if (QuestPage.IsValid)
							{
								EQ2UIElement QuestAcceptButton = QuestPage.Child("button", "RewardPack.Accept");
								if (QuestAcceptButton.IsValid)
								{
									Program.Log("Automatically accepting quest \"{0}\"...", s_EQ2.PendingQuestName);
									s_EQ2.AcceptPendingQuest();
									QuestAcceptButton.LeftClick();
								}
							}
						}
					}

					unchecked { s_lFrameCount++; }

					/// If the subclass changes (startup, betrayal, etc), resync.
					/// The null check on SubClass is because it comes up as null when reviving.
					/// s_Controller is guaranteed to be non-null after this block (otherwise the program would have exited).
					/// TODO: State variables contained in the prior controller will be lost. Maybe make them static?
					if (!string.IsNullOrEmpty(Me.SubClass) && Me.SubClass != strLastClass)
					{
						Program.Log("New class found: \"{0}\"", Me.SubClass);
						strLastClass = Me.SubClass;
						s_bRefreshKnowledgeBook = true;

						switch (strLastClass.ToLower())
						{
							case "coercer": s_Controller = new CoercerController(); break;
							case "illusionist": s_Controller = new IllusionistController(); break;
							case "mystic": s_Controller = new MysticController(); break;
							case "templar": s_Controller = new TemplarController(); break;
							case "troubador": s_Controller = new TroubadorController(); break;
							case "warden": s_Controller = new WardenController(); break;
							case "warlock": s_Controller = new WarlockController(); break;
							case "wizard": s_Controller = new WizardController(); break;
							default:
							{
								Program.Log("Unrecognized or unsupported subclass type: {0}.", Me.SubClass);
								Program.Log("Will use generic controller without support for spells or combat arts.");
								s_Controller = new PlayerController();
								break;
							}
						}

						/// Build the name of the INI file.
						string strFileName = string.Format("{0}.{1}.ini", ServerName, Name);
						s_strCurrentINIFilePath = Path.Combine(Program.ConfigurationFolderPath, strFileName);

						if (File.Exists(s_strCurrentINIFilePath))
							s_Controller.ReadINISettings();

						s_Controller.WriteINISettings();

						SetWindowText(string.Format("{0} ({1})", Name, Me.SubClass));

						ApplyGameSettings();
					}

					/// If the size of the knowledge book changes, defer a resync.
					/// NOTE: If the user equips or unequips an ability-changing item,
					/// the ability table will be hosed but we'll have no way of knowing to force a refresh.
					if (s_Controller.AbilityCountChanged)
						s_bRefreshKnowledgeBook = true;

					/// Only if the knowledge book is intact can we safely assume that regular actions are OK.
					/// DoNextAction() might set s_bRefreshKnowledgeBook to true.  This is fine.
					if (!s_bRefreshKnowledgeBook)
					{
						s_Controller.DoNextAction();
						s_Controller.UpdateEndOfRoundStatistics();
					}

					/// Only check for camping or AFK every 5th frame.
					if (s_Controller.m_bKillBotWhenCamping && (s_lFrameCount % 5) == 0 && (Me.IsCamping))
					{
						Program.Log("Camping detected; aborting bot!");
						s_bContinueBot = false;
					}
				}
				finally
				{
					Frame.Unlock();
				}

				/// If we have to refresh the knowledge book, then do it outside of the main lock.
				/// This is because we'll use frame waits and can't risk breaking cached data.
				if (s_bRefreshKnowledgeBook)
				{
					s_Controller.RefreshKnowledgeBook();
					s_bRefreshKnowledgeBook = false;
				}

				/// Skip frames as configured.
				for (int iIndex = 0; iIndex < s_Controller.m_iFrameSkip; iIndex++)
				{
					Frame.Wait(false);
					unchecked { s_lFrameCount++; }
				}
			}
			while (s_bContinueBot);

			/// Don't overwrite files that already exist unless told to; people might have special comments in place.
			if (!File.Exists(s_strCurrentINIFilePath) || s_Controller.m_bWriteBackINI)
			{
				s_Controller.WriteINISettings();
			}

			return;
		}

		/************************************************************************************/
		protected static void ApplyGameSettings()
		{
			/// We could do this once at the beginning, but I've seen it not take.
			Program.Log("Applying preferential account settings (music volume, personal torch, welcome screen).");
			RunCommand("/music_volume 0");
			RunCommand("/r_personal_torch off");
			RunCommand("/cl_show_welcome_screen_on_startup 0");
			return;
		}

		/************************************************************************************/
		protected static void SetWindowText(string strText)
		{
			s_strNewWindowTitle = strText;
			return;
		}

		/************************************************************************************/
		/// <summary>
		/// I hate trying to remember the syntax, so I hid it behind this function.
		/// </summary>
		/// <param name="bThrottled">Whether or not to prevent immediate duplicates of the same command until a certain time has passed.</param>
		/// <param name="strCommand"></param>
		protected static void RunCommand(double fBlockageSeconds, string strCommand, params object[] aobjParams)
		{
			if (string.IsNullOrEmpty(strCommand))
				return;

			try
			{
				string strFinalCommand = string.Empty;

				if (aobjParams.Length == 0)
					strFinalCommand += string.Format("{0}", strCommand);
				else
					strFinalCommand += string.Format(strCommand, aobjParams);

				/// Throttle it only if the parameter says so.
				if (fBlockageSeconds > 0.0 && m_RecentThrottledCommandIndex.ContainsKey(strFinalCommand))
				{
					if (DateTime.Now > m_RecentThrottledCommandIndex[strFinalCommand])
						m_RecentThrottledCommandIndex.Remove(strFinalCommand);
					else
					{
						Program.Log("Throttled command blocked: {0}", strFinalCommand);
						return;
					}
				}

				using (new FrameLock(true))
				{
					Program.Log("Executing: {0}", strFinalCommand);

					/// Break the command up as if it were a custom one.
					List<string> astrParameters = new List<string>(strFinalCommand.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
					string strCustomCommand = astrParameters[0].Replace("/", "");
					astrParameters.RemoveAt(0);

					/// We have to manually process custom commands, EQ2Execute does not handle them.
					if (m_RegisteredCustomSlashCommands.Contains(strCustomCommand))
					{
						if (s_Controller != null && UpdateStaticGlobals())
							s_Controller.OnCustomSlashCommand(strCustomCommand, astrParameters.ToArray());
					}
					else
						s_Extension.EQ2Execute(strFinalCommand);
				}

				m_RecentThrottledCommandIndex.Add(strFinalCommand, DateTime.Now + TimeSpan.FromSeconds(fBlockageSeconds));
			}
			catch
			{
			}

			return;
		}

		/************************************************************************************/
		/// <summary>
		/// This version of RunCommand DEFAULTS TO NON-THROTTLED.
		/// </summary>
		/// <param name="strCommand"></param>
		/// <param name="aobjParams"></param>
		protected static void RunCommand(string strCommand, params object[] aobjParams)
		{
			RunCommand(0, strCommand, aobjParams);
			return;
		}

		/************************************************************************************/
		protected static void ApplyVerb(int iActorID, string strVerb)
		{
			RunCommand(5, "/apply_verb {0} {1}", iActorID, strVerb);
			return;
		}

		/************************************************************************************/
		protected static void ApplyVerb(Actor ThisActor, string strVerb)
		{
			Program.Log("Applying verb \"{0}\" on actor \"{1}\" (ID: \"{2}\").", strVerb, ThisActor.Name, ThisActor.ID);
			ApplyVerb(ThisActor.ID, strVerb);
			return;
		}

		/************************************************************************************/
		protected static IEnumerable<Maintained> EnumMaintained()
		{
			for (int iIndex = 1; iIndex <= Me.CountMaintained; iIndex++)
				yield return Me.Maintained(iIndex);
		}

		/************************************************************************************/
		protected static IEnumerable<GroupMember> EnumGroupMembers()
		{
			/// Referring to group member #0 is shady but it's useful enough for us to continue doing it.
			if (IsInGroup || IsInRaid)
			{
				for (int iIndex = 0; iIndex <= 5; iIndex++)
				{
					GroupMember ThisMember = Me.Group(iIndex);
					if (ThisMember != null && !string.IsNullOrEmpty(ThisMember.Name))
						yield return ThisMember;
				}
			}
			else
				yield return Me.Group(0);
		}

		/************************************************************************************/
		protected static IEnumerable<GroupMember> EnumRaidMembers()
		{
			if (IsInRaid)
			{
				/// Documentation says to iterate through all 24 even if we have less than 24.
				for (int iIndex = 1; iIndex <= 24; iIndex++)
				{
					GroupMember ThisMember = Me.Raid(iIndex, false);
					if (ThisMember != null && !string.IsNullOrEmpty(ThisMember.Name))
						yield return ThisMember;
				}
			}
			else
			{
				foreach (GroupMember ThisMember in EnumGroupMembers())
					yield return ThisMember;
			}
		}

		/************************************************************************************/
		protected static IEnumerable<Ability> EnumAbilities()
		{
			for (int iIndex = 1; iIndex <= AbilityCount; iIndex++)
				yield return Me.Ability(iIndex);
		}

		/************************************************************************************/
		protected static IEnumerable<Actor> EnumActors(params string[] astrParams)
		{
			s_EQ2.CreateCustomActorArray(astrParams);

			for (int iIndex = 1; iIndex <= s_EQ2.CustomActorArraySize; iIndex++)
				yield return s_Extension.CustomActor(iIndex);
		}

		/************************************************************************************/
		protected static IEnumerable<Actor> EnumActorsInRadius(double fRadius)
		{
			return EnumActors("byDist", fRadius.ToString());
		}

		/************************************************************************************/
		/// <summary>
		/// Frame lock is assumed to be held before this function is called.
		/// </summary>
		protected static Actor GetNonPetActor(string strName)
		{
			Actor PlayerActor = s_Extension.Actor(strName);

			/// Try again if it's invalid or it's a pet.
			if (!PlayerActor.IsValid || PlayerActor.IsAPet)
			{
				PlayerActor = s_Extension.Actor(strName, "notid", PlayerActor.ID.ToString());
			}

			if (!PlayerActor.IsValid || PlayerActor.IsAPet)
				PlayerActor = null;

			return PlayerActor;
		}

		/************************************************************************************/
		protected static Actor GetPlayerActor(string strName)
		{
			if (strName == Name)
				return MeActor;

			string strLowerCaseName = strName.ToLower();
			foreach (Actor ThisActor in EnumActors())
				if (ThisActor.Name.ToLower() == strLowerCaseName && ThisActor.Type == "PC")
					return ThisActor;

			return null;
		}

		/************************************************************************************/
		protected static Actor GetActor(int iActorID)
		{
			try
			{
				return s_Extension.Actor(iActorID);
			}
			catch
			{
				Program.Log("Exception thrown when looking up actor {0}.", iActorID);
				return null;
			}
		}

		/************************************************************************************/
		protected static Actor GetActor(string strActorID)
		{
			try
			{
				return s_Extension.Actor(strActorID);
			}
			catch
			{
				Program.Log("Exception thrown when looking up actor {0}.", strActorID);
				return null;
			}
		}

		/************************************************************************************/
		/// <summary>
		/// Using Thread.Sleep() during a frame lock, locks up the client.
		/// </summary>
		/// <param name="ThisTimeSpan"></param>
		protected static void DeadWait(TimeSpan ThisTimeSpan)
		{
			Thread.Sleep((int)ThisTimeSpan.TotalMilliseconds);
			return;
		}

		/************************************************************************************/
		protected static void FrameWait(TimeSpan ThisTimeSpan)
		{
			DateTime WaitEndTime = DateTime.Now + ThisTimeSpan;

			while (DateTime.Now < WaitEndTime)
				Frame.Wait(false);

			return;
		}

		/************************************************************************************/
		protected static void PressAndHoldKey(string strKey)
		{
			string strIndexedKey = strKey.ToLower().Trim();
			if (!s_PressedKeys.Contains(strIndexedKey))
			{
				Program.Log("Pressing and holding keyboard key: {0}", strKey);
				s_PressedKeys.Add(strIndexedKey);
			}

			/// Reapply the key even if it was applied before.
			/// Sometimes with window focus changes, the press gets lost.
			LavishScriptAPI.LavishScript.ExecuteCommand("press -hold " + strKey);
			return;
		}

		/************************************************************************************/
		/// <summary>
		/// Releases a key but only if we remember pressing it in the first place.
		/// This prevents interference with user action.
		/// </summary>
		protected static void ReleaseKey(string strKey)
		{
			string strIndexedKey = strKey.ToLower().Trim();
			if (s_PressedKeys.Contains(strIndexedKey))
			{
				Program.Log("Releasing keyboard key: {0}", strKey);
				LavishScriptAPI.LavishScript.ExecuteCommand("press -release " + strKey);
				s_PressedKeys.Remove(strIndexedKey);
			}
			return;
		}

		/************************************************************************************/
		protected static void ReleaseAllKeys()
		{
			foreach (string strThisKey in s_PressedKeys)
			{
				Program.Log("Releasing keyboard key: {0}", strThisKey);
				LavishScriptAPI.LavishScript.ExecuteCommand("press -release " + strThisKey);
			}
			s_PressedKeys.Clear();
			return;
		}

		/************************************************************************************/
		protected static void RegisterCustomSlashCommands(params string[] astrCommandNames)
		{
			foreach (string strCommand in astrCommandNames)
			{
				string strActualCommand = strCommand.Trim().ToLower();
				if (!m_RegisteredCustomSlashCommands.Contains(strActualCommand))
				{
					m_RegisteredCustomSlashCommands.Add(strActualCommand);
					LavishScript.Commands.AddCommand(strActualCommand, s_CustomSlashCommandDelegate);
				}
			}
			return;
		}

		/************************************************************************************/
		private static void OnChoiceWindowAppeared_EventHandler(object sender, LSEventArgs e)
		{
			try
			{
				if (s_Controller != null)
				{
					using (new FrameLock(true))
					{
						UpdateStaticGlobals();
						s_Controller.OnChoiceWindowAppeared(s_Extension.ChoiceWindow());
					}
				}
			}
			catch (Exception ex)
			{
				/// Do nothing. But at least the bot doesn't crash!
				Program.OnUnhandledException(ex);
			}

			return;
		}

		/************************************************************************************/
		private static void OnRewardWindowAppeared_EventHandler(object sender, LSEventArgs e)
		{
			try
			{
				if (s_Controller != null)
				{
					using (new FrameLock(true))
					{
						UpdateStaticGlobals();
						s_Controller.OnRewardWindowAppeared(s_Extension.RewardWindow());
					}
				}
			}
			catch (Exception ex)
			{
				/// Do nothing. But at least the bot doesn't crash!
				Program.OnUnhandledException(ex);
			}

			return;
		}

		/************************************************************************************/
		private static void OnIncomingChatText_EventHandler(object sender, LSEventArgs e)
		{
			try
			{
				int iChannel = int.Parse(e.Args[0]);
				string strMessage = e.Args[1];
				string strFrom = e.Args[2];

				if (s_Controller != null)
				{
					using (new FrameLock(true))
					{
						UpdateStaticGlobals();
						s_Controller.OnIncomingChatText(iChannel, strFrom, strMessage);
					}
				}
			}
			catch (Exception ex)
			{
				/// Do nothing. But at least the bot doesn't crash!
				Program.OnUnhandledException(ex);
			}

			return;
		}

		/************************************************************************************/
		/// <summary>
		/// Handles every string that can possibly appear in a chat window.
		/// Might be useful for in-process parsing down the road!
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnIncomingText_EventHandler(object sender, LSEventArgs e)
		{
			try
			{
				if (s_Controller != null)
				{
					using (new FrameLock(true))
					{
						UpdateStaticGlobals();
						s_Controller.OnIncomingText(PlayerController.ChatChannel.NonChat, string.Empty, string.Empty, e.Args[0]);
					}
				}
			}
			catch (Exception ex)
			{
				/// Do nothing. But at least the bot doesn't crash!
				Program.OnUnhandledException(ex);
			}

			return;
		}

		/************************************************************************************/
		private static void OnFrame_EventHandler(object sender, LSEventArgs e)
		{
			if (!string.IsNullOrEmpty(s_strNewWindowTitle))
			{
				string strCommand = string.Format("windowtext {0}", s_strNewWindowTitle);

				using (new FrameLock(true))
					LavishScript.ExecuteCommand(strCommand);

				s_strNewWindowTitle = null;
			}
			return;
		}

		/************************************************************************************/
		private static int OnLavishScriptCommand(string[] astrArgs)
		{
			try
			{
				if (s_Controller != null)
				{
					List<string> astrArgList = new List<string>(astrArgs);
					astrArgList.RemoveAt(0);
					using (new FrameLock(true))
					{
						if (UpdateStaticGlobals())
							s_Controller.OnCustomSlashCommand(astrArgs[0].ToLower(), astrArgList.ToArray());
					}
				}
			}
			catch (Exception e)
			{
				Program.OnUnhandledException(e);
			}

			return 0;
		}

		/************************************************************************************/
		protected static void ToggleSpeechSynthesizer(bool bActivate, int iVolume, string strVoiceProfile)
		{
			if (bActivate)
			{
				if (s_SpeechSynthesizer == null)
					s_SpeechSynthesizer = new SpeechSynthesizer();
				s_SpeechSynthesizer.Volume = iVolume;

				try
				{
					s_SpeechSynthesizer.SelectVoice(strVoiceProfile);
				}
				catch
				{
					/// If no voice is found, use the first installed one we find.
					foreach (InstalledVoice ThisVoice in s_SpeechSynthesizer.GetInstalledVoices())
					{
						s_SpeechSynthesizer.SelectVoice(ThisVoice.VoiceInfo.Name);
						break;
					}
				}
			}
			else
			{
				if (s_SpeechSynthesizer != null)
				{
					s_SpeechSynthesizer.Dispose();
					s_SpeechSynthesizer = null;
				}
			}
		}

		/************************************************************************************/
		protected static void SayText(string strFormat, params object[] aobjParams)
		{
			string strOutput = string.Format(strFormat, aobjParams);
			if (string.IsNullOrEmpty(strOutput))
				return;

			if (s_Controller != null && s_Controller.m_bUseVoiceSynthesizer && s_SpeechSynthesizer != null)
				s_SpeechSynthesizer.SpeakAsync(string.Format(strFormat, aobjParams));

			return;
		}
	}
}