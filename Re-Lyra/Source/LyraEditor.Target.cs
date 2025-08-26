// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;
using System.Collections.Generic;

public class LyraEditorTarget : TargetRules
{
	public LyraEditorTarget( TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		ExtraModuleNames.AddRange(new string[] {
            "LyraGame",
			"LyraEditor"
        });
		LyraGameTarget.ApplySharedLyraTargetSettings(this);
		
		// 如果不是构建所有模块（即选择性构建），则禁止原生指针成员(否则UHT报错应当使用TObjectPtr管理) 提高代码安全性，防止潜在的内存访问问题
		if (!bBuildAllModules)
		{
			NativePointerMemberBehaviorOverride = PointerMemberBehavior.Disallow;
		}
		
		//触屏游戏开发功能，同时与“虚幻远程 2”应用配合使用
		EnablePlugins.Add("RemoteSession");
	}
}
