// Copyright Epic Games, Inc. All Rights Reserved.
#include "Modules/ModuleManager.h"
class FLyraGameModlue : public FDefaultGameModuleImpl
{
    virtual void StartupModule() override
    {
    }
    virtual void ShutdownModule() override
    {
    }
};
IMPLEMENT_PRIMARY_GAME_MODULE(FLyraGameModlue, LyraGame, "LyraGame");
