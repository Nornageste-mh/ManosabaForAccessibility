using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GigaCreation.Essentials.Ui;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Collections.Generic;
using Naninovel;
using Naninovel.UI;
using UnityEngine;

namespace GigaCreation.NaninovelExtender.Ui;

public class UiManagerExtended : UIManager
{
	private static readonly IntPtr NativeFieldInfoPtr__naniScriptPlayer;

	private static readonly IntPtr NativeFieldInfoPtr__currentModalUis;

	private static readonly IntPtr NativeFieldInfoPtr__modalContainer;

	private static readonly IntPtr NativeFieldInfoPtr__containerGroup;

	private static readonly IntPtr NativeMethodInfoPtr_get_ModalContainer_Private_get_GameObject_0;

	private static readonly IntPtr NativeMethodInfoPtr_get_ContainerGroup_Private_get_CanvasGroup_0;

	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_UIConfiguration_IResourceProviderManager_ILocalizationManager_ICommunityLocalization_ICameraManager_IInputManager_IScriptPlayer_0;

	private static readonly IntPtr NativeMethodInfoPtr_AddModalUI_Public_Virtual_Void_IManagedUI_0;

	private static readonly IntPtr NativeMethodInfoPtr_RemoveModalUI_Public_Virtual_Void_IManagedUI_0;

	private static readonly IntPtr NativeMethodInfoPtr_SetCancelEventBlocked_Private_Static_Void_Boolean_Il2CppReferenceArray_1_ICancelEventBlockable_0;

	private static readonly IntPtr NativeMethodInfoPtr_IsModal_Protected_Virtual_Boolean_IManagedUI_0;

	private static readonly IntPtr NativeMethodInfoPtr_ToggleUI_Protected_Virtual_Void_0;

	public unsafe IScriptPlayer _naniScriptPlayer
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__naniScriptPlayer);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IScriptPlayer>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__naniScriptPlayer)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)scriptPlayer));
		}
	}

	public unsafe IList<IManagedUI> _currentModalUis
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__currentModalUis);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IList<IManagedUI>>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__currentModalUis)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)list));
		}
	}

	public unsafe GameObject _modalContainer
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__modalContainer);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<GameObject>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__modalContainer)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)gameObject));
		}
	}

	public unsafe CanvasGroup _containerGroup
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__containerGroup);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<CanvasGroup>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__containerGroup)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)canvasGroup));
		}
	}

	public unsafe GameObject ModalContainer
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 924990, RefRangeEnd = 924992, XrefRangeStart = 924981, XrefRangeEnd = 924990, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ModalContainer_Private_get_GameObject_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<GameObject>(intPtr) : null;
		}
	}

	public unsafe CanvasGroup ContainerGroup
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 925003, RefRangeEnd = 925005, XrefRangeStart = 924992, XrefRangeEnd = 925003, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ContainerGroup_Private_get_CanvasGroup_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<CanvasGroup>(intPtr) : null;
		}
	}

	static UiManagerExtended()
	{
		Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.NaninovelExtender.UI.dll", "GigaCreation.NaninovelExtender.Ui", "UiManagerExtended");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr);
		NativeFieldInfoPtr__naniScriptPlayer = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, "_naniScriptPlayer");
		NativeFieldInfoPtr__currentModalUis = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, "_currentModalUis");
		NativeFieldInfoPtr__modalContainer = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, "_modalContainer");
		NativeFieldInfoPtr__containerGroup = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, "_containerGroup");
		NativeMethodInfoPtr_get_ModalContainer_Private_get_GameObject_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663302);
		NativeMethodInfoPtr_get_ContainerGroup_Private_get_CanvasGroup_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663303);
		NativeMethodInfoPtr__ctor_Public_Void_UIConfiguration_IResourceProviderManager_ILocalizationManager_ICommunityLocalization_ICameraManager_IInputManager_IScriptPlayer_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663304);
		NativeMethodInfoPtr_AddModalUI_Public_Virtual_Void_IManagedUI_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663305);
		NativeMethodInfoPtr_RemoveModalUI_Public_Virtual_Void_IManagedUI_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663306);
		NativeMethodInfoPtr_SetCancelEventBlocked_Private_Static_Void_Boolean_Il2CppReferenceArray_1_ICancelEventBlockable_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663307);
		NativeMethodInfoPtr_IsModal_Protected_Virtual_Boolean_IManagedUI_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663308);
		NativeMethodInfoPtr_ToggleUI_Protected_Virtual_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr, 100663309);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 925005, XrefRangeEnd = 925012, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe UiManagerExtended(UIConfiguration config, IResourceProviderManager resources, ILocalizationManager l10n, ICommunityLocalization communityL10n, ICameraManager cameraManager, IInputManager inputManager, IScriptPlayer scriptPlayer)
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<UiManagerExtended>.NativeClassPtr))
	{
		IntPtr* ptr = stackalloc IntPtr[7];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)config);
		*(IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)resources);
		*(IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)l10n);
		*(IntPtr*)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)communityL10n);
		*(IntPtr*)((byte*)ptr + checked((nuint)4u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)cameraManager);
		*(IntPtr*)((byte*)ptr + checked((nuint)5u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)inputManager);
		*(IntPtr*)((byte*)ptr + checked((nuint)6u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)scriptPlayer);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_UIConfiguration_IResourceProviderManager_ILocalizationManager_ICommunityLocalization_ICameraManager_IInputManager_IScriptPlayer_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 925012, XrefRangeEnd = 925058, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void AddModalUI(IManagedUI managedUI)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)managedUI);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_AddModalUI_Public_Virtual_Void_IManagedUI_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 925058, XrefRangeEnd = 925095, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void RemoveModalUI(IManagedUI managedUI)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)managedUI);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_RemoveModalUI_Public_Virtual_Void_IManagedUI_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 925100, RefRangeEnd = 925104, XrefRangeStart = 925095, XrefRangeEnd = 925100, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void SetCancelEventBlocked(bool isBlocked, [Optional] Il2CppReferenceArray<ICancelEventBlockable> blockables)
	{
		if (blockables == null)
		{
			blockables = new Il2CppReferenceArray<ICancelEventBlockable>(0L);
		}
		IntPtr* ptr = stackalloc IntPtr[2];
		*ptr = (nint)(&isBlocked);
		*(IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)blockables);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetCancelEventBlocked_Private_Static_Void_Boolean_Il2CppReferenceArray_1_ICancelEventBlockable_0, (IntPtr)0, (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 925104, XrefRangeEnd = 925108, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override bool IsModal(IManagedUI managedUI)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)managedUI);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_IsModal_Protected_Virtual_Boolean_IManagedUI_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		return *(bool*)IL2CPP.il2cpp_object_unbox(intPtr);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 925108, XrefRangeEnd = 925116, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void ToggleUI()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = null;
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_ToggleUI_Protected_Virtual_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public static void SetCancelEventBlocked(bool isBlocked, params ICancelEventBlockable[] blockables)
	{
		SetCancelEventBlocked(isBlocked, new Il2CppReferenceArray<ICancelEventBlockable>(blockables));
	}

	public UiManagerExtended(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
