// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using UnrealBuildTool;
using System.Collections.Generic;
using System.IO;
using EpicGames.Core;
using Microsoft.Extensions.Logging;
using UnrealBuildBase;

public class LyraGameTarget : TargetRules
{
	public LyraGameTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.V5;
		IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_6;
		ExtraModuleNames.Add("LyraGame");
	}
	
	//日志提示标识
	private static bool bHasWarnedAboutShared = false;
	
	//抽象出的共享版本设置
	internal static void ApplySharedLyraTargetSettings(TargetRules Target)
	{
		//用于记录与该目标相关数据的日志，在规则编译器运行之前进行设置
		ILogger Logger = Target.Logger;
		Target.DefaultBuildSettings = BuildSettingsVersion.V5;
		Target.IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_6;
		
		//测试配置
		bool bIsTest = Target.Configuration==UnrealTargetConfiguration.Test;
		
		//发行配置
		bool bIsShipping = Target.Configuration==UnrealTargetConfiguration.Shipping;
		
		//专属服务器
		bool bIsDedicatedServer=Target.Type==TargetType.Server;
		
		//引擎的二进制文件和中间文件是针对此目标特定的
		if (Target.BuildEnvironment == TargetBuildEnvironment.Unique)
		{
			//由目标程序所使用的C++警告设置对象，输出警告级别为错误
			Target.CppCompileWarningSettings.ShadowVariableWarningLevel=WarningLevel.Error;
			//是否开启测试/发布版本的日志记录功能
			Target.bUseLoggingInShipping = true;
			//是否在测试配置中跟踪RHI资源所有者
			Target.bTrackRHIResourceInfoForTest=true;
			
			//发行模式且非专属服务器
			if (bIsShipping && !bIsDedicatedServer)
			{
				//是否允许通过引擎配置来决定是否能够加载未验证证书
				Target.bDisableUnverifiedCertificates=true;
			}

			if (bIsShipping || bIsTest)
			{
				//是否在烘焙版本中加载生成的ini文件
				Target.bAllowGeneratedIniWhenCooked = false;
				//在烘焙版本中是否要加载非ufs格式的ini文件
				Target.bAllowNonUFSIniWhenCooked = false;
			}

			//非编辑器模式下
			if (Target.Type != TargetType.Editor)
			{
				//运行时不使用追踪功能，仅用于制作精美效果图，且该库体积很大
				Target.DisablePlugins.Add("OpenImageDenoise");
				//减少资产注册表中始终加载数据所占用的内存，增加一些计算资源消耗较大的查询操作
				Target.GlobalDefinitions.Add("UE_ASSETREGISTRY_INDIRECT_ASSETDATA_POINTERS=1");
			}
			
			LyraGameTarget.ConfigureGameFeaturePlugins(Target);
		}
		else
		{
			//仅在编辑器或者独占构建环境中有效
			if (Target.Type == TargetType.Editor)
			{
				LyraGameTarget.ConfigureGameFeaturePlugins(Target);
			}
			else
			{
				//非编辑器环境下弹出警告
				if (!bHasWarnedAboutShared)
				{
					bHasWarnedAboutShared = true;
					Logger.LogWarning("LyraGameEOS and dynamic target options are disabled when packaging form an installed version of the engine");
				}
			}
		}
	}
	private static Dictionary<string,JsonObject> AllPluginRootJsonObjectsByName=new Dictionary<string,JsonObject>();
	//配置GameFeature插件
	static public void ConfigureGameFeaturePlugins(TargetRules Target)
	{
		ILogger Logger = Target.Logger;
		Log.TraceInformationOnce("Compiling GameFeaturePlugins in branch{0}", Target.Version.BranchName);
		bool bBuildAllGameFerturePlugins=ShouldEnableAllGameFeaturePlugins(Target);
		
		//创建文件引用容器
		List<FileReference> CombinePluginList=new List<FileReference>();
		
		//获取所有GameFeature的插件引用
		List<DirectoryReference> GameFeaturePluginRoots= Unreal.GetExtensionDirs(Target.ProjectFile.Directory,Path.Combine("Plugins","GameFeatures"));
		//遍历填充容器
		foreach (DirectoryReference SearchDir in GameFeaturePluginRoots)
		{
			CombinePluginList.AddRange(PluginsBase.EnumeratePlugins(SearchDir));
		}
		//如果启用了该插件
		if (CombinePluginList.Count > 0)
		{
			//记录所有引用到的插件，因为插件可能存在外部依赖关系
			Dictionary<string,List<string>>AllPluginReferencesByName=new Dictionary<string, List<string>>();
			foreach (FileReference PluginFile in CombinePluginList)
			{
				//判断该插件是否真实存在
				if (PluginFile != null && FileReference.Exists(PluginFile))
				{
					//初始设置不使用/强制关闭
					bool bEnabled = false;
					bool bForceDisable = false;

					try
					{
						//获取并添加到插件的JsonObject字典中
						JsonObject RawObject;
						if (!AllPluginRootJsonObjectsByName.TryGetValue(PluginFile.GetFileNameWithoutExtension(), out RawObject))
						{
							RawObject=JsonObject.Read(PluginFile);
							AllPluginRootJsonObjectsByName.Add(PluginFile.GetFileNameWithoutExtension(), RawObject);
						}
						//确认游戏功能插件默认均已禁用，否则输出警告
						bool bEnabledByDefault=false;
						if (!RawObject.TryGetBoolField("EnabledByDefault", out bEnabledByDefault) || bEnabledByDefault)
						{
							Log.TraceInformation("GameFeaturePlugin {0} does not set EnabledByDefault to false.", PluginFile.GetFileNameWithoutExtension());
						}
						//游戏功能插件均要设置为明确加载，否则输出警告
						bool bExplicitlyLoaded = false;
						if (!RawObject.TryGetBoolField("ExplicitlyLoaded", out bExplicitlyLoaded) || !bExplicitlyLoaded)
						{
							Log.TraceInformation("GameFeaturePlugin {0} does not set ExplicitlyLoaded to true.", PluginFile.GetFileNameWithoutExtension());
						}
						//可以在此处添加额外字段,根据项目情况添加(例如插件版本等等)
						if (bBuildAllGameFerturePlugins)
						{
							bEnabled = true;
						}
						//防止非编译器版本中使用仅适用于编辑器的功能插件
						bool bEditorOnly = false;
						if (RawObject.TryGetBoolField("EditorOnly", out bEditorOnly))
						{
							if (bEditorOnly && (Target.Type != TargetType.Editor) && !bBuildAllGameFerturePlugins)
							{
								//该插件仅适用于编辑器使用，而目前正在构造非编译器版本，因此禁用该插件
								bForceDisable = true;
							}
						}
						else
						{
							//编辑器专用(可选)
						}
						// 对于特定分支的插件启用
						string RestrictToBranch;
						if (RawObject.TryGetStringField("RestrictToBranch", out RestrictToBranch))
						{
							if (!Target.Version.BranchName.Equals(RestrictToBranch, StringComparison.OrdinalIgnoreCase))
							{
								//特定分支启用的，此情况为非特定分支
								bForceDisable = true;
								Logger.LogDebug("GameFeaturePlugin {Name} was marked as restricted to other branches. Disabling.",PluginFile.GetFileNameWithoutExtension());
							}
							else
							{
								Logger.LogDebug("GameFeaturePlugin {Name} was marked as restricted to this branches. Leaving enabled.",PluginFile.GetFileNameWithoutExtension());
								
							}
						}
						
						//可以将插件标记为永不编译，将覆盖上述配置
						bool bNeverBuild = false;
						if (RawObject.TryGetBoolField("NeverBuild", out bNeverBuild) && bNeverBuild)
						{
							bForceDisable = true;
							Logger.LogDebug("GameFeaturePlugin {Name} was marked as NeverBuild. Disabling.",PluginFile.GetFileNameWithoutExtension());
						}
						
						//记录插件引用信息，以便于进行验证
						JsonObject[] PluginReferencesArray;
						//遍历该字段，如果开启Plugin字段，则放入容器
						if (RawObject.TryGetObjectArrayField("Plugins", out PluginReferencesArray))
						{
							foreach (JsonObject ReferenceObject in PluginReferencesArray)
							{
								bool bRefEnabled=false;
								if (ReferenceObject.TryGetBoolField("Enabled", out bRefEnabled) && bRefEnabled)
								{
									string PluginReferenceName;
									if (ReferenceObject.TryGetStringField("Name", out PluginReferenceName))
									{
										string ReferencerName=PluginFile.GetFileNameWithoutExtension();
									
										if (!AllPluginReferencesByName.ContainsKey(ReferencerName))
										{
											AllPluginReferencesByName[ReferencerName]=new List<string>();
										}
										AllPluginReferencesByName[ReferencerName].Add(PluginReferenceName);
									}
								}
							}
						}
					}
					catch (Exception ParseException)
					{
						Logger.LogWarning("Failed to parse GameFeaturePlugin file {Name} , Disabling.Exception{1}",PluginFile.GetFileNameWithoutExtension(), ParseException.Message);
						bForceDisable = true;
					}
					
					//禁用状态优先于启动状态
					if (bForceDisable)
					{
						bEnabled = false;
					}
					
					//输出此插件的最终决策结果
					Logger.LogDebug("Configuring GameFeaturePlugins() has decided to {Action} feature {Name}",bEnabled?"enable":(bForceDisable?"disable":"ignore"),PluginFile.GetFileNameWithoutExtension());
					
					//启用或禁用
					if (bEnabled)
					{
						Target.EnablePlugins.Add(PluginFile.GetFileNameWithoutExtension());
					}
					else if (bForceDisable)
					{
						Target.DisablePlugins.Add(PluginFile.GetFileNameWithoutExtension());
					}
				}
			}
		}
	}
	//是否应当启用所有GameFeature插件
	static public bool ShouldEnableAllGameFeaturePlugins(TargetRules Target)
	{
		if(Target.Type==TargetType.Editor)
		{
			//如果设置为true，编辑器将构建所有游戏功能插件，但这些插件是否会被全部加载取决于具体情况
			//这样就可以在编辑器中启用插件，而无需编译代码
			return true;
		}
		bool bIsBuildMachine=(Environment.GetEnvironmentVariable("IsBuildMachine")=="1");
		if (bIsBuildMachine)
		{
			//为构建机器启用所有插件
			return true;
		}
		// 默认情况下，将使用插件浏览器在编辑器中设置的默认插件规则
		// 在安装在启动器中的代码，这段代码可能不会执行
		return false;
	}
}
