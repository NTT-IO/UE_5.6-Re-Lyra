// Copyright Epic Games, Inc. All Rights Reserved.

#include "LyraEditor.h"
#include "Modules/ModuleManager.h"
class FLyraEditorModule : public FDefaultModuleImpl
{
    virtual void StartupModule() override
    {
    }
    virtual void ShutdownModule() override
    {
    }
};
IMPLEMENT_MODULE(FDefaultGameModuleImpl, LyraEditor);
