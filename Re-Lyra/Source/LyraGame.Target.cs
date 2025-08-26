// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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
	}
}
