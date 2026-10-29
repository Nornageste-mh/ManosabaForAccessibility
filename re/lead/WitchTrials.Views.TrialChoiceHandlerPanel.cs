using System;
using System.Runtime.CompilerServices;
using GigaCreation.NaninovelExtender.Ui;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace WitchTrials.Views;

public class TrialChoiceHandlerPanel : ChoiceHandlerPanelModified
{
	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	static TrialChoiceHandlerPanel()
	{
		Il2CppClassPointerStore<TrialChoiceHandlerPanel>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "WitchTrials.Views", "TrialChoiceHandlerPanel");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<TrialChoiceHandlerPanel>.NativeClassPtr);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<TrialChoiceHandlerPanel>.NativeClassPtr, 100664302);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe TrialChoiceHandlerPanel()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<TrialChoiceHandlerPanel>.NativeClassPtr))
	{
		IntPtr* ptr = null;
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public TrialChoiceHandlerPanel(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
