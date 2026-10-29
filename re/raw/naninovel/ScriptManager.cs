using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;

namespace Naninovel;

public class ScriptManager : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr__Configuration_k__BackingField;

	private static readonly System.IntPtr NativeFieldInfoPtr__TotalCommandsCount_k__BackingField;

	private static readonly System.IntPtr NativeFieldInfoPtr_providers;

	private static readonly System.IntPtr NativeFieldInfoPtr_scriptLoader;

	private static readonly System.IntPtr NativeFieldInfoPtr_extScriptLoader;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_Configuration_Public_Virtual_New_get_ScriptsConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_ScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_TotalCommandsCount_Public_Virtual_New_get_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_set_TotalCommandsCount_Private_set_Void_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_ScriptsConfiguration_IResourceProviderManager_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_InitializeService_Public_Virtual_New_UniTask_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ResetService_Public_Virtual_New_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_DestroyService_Public_Virtual_New_Void_0;

	public unsafe ScriptsConfiguration _Configuration_k__BackingField
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__Configuration_k__BackingField);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ScriptsConfiguration>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__Configuration_k__BackingField)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)scriptsConfiguration));
		}
	}

	public unsafe int _TotalCommandsCount_k__BackingField
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__TotalCommandsCount_k__BackingField);
			return *(int*)num;
		}
		set
		{
			*(int*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__TotalCommandsCount_k__BackingField)) = num;
		}
	}

	public unsafe IResourceProviderManager providers
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_providers);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<IResourceProviderManager>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_providers)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)resourceProviderManager));
		}
	}

	public unsafe ResourceLoader<Script> scriptLoader
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_scriptLoader);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResourceLoader<Script>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_scriptLoader)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)resourceLoader));
		}
	}

	public unsafe ResourceLoader<Script> extScriptLoader
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_extScriptLoader);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResourceLoader<Script>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_extScriptLoader)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)resourceLoader));
		}
	}

	public unsafe virtual ScriptsConfiguration Configuration
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 374, RefRangeEnd = 376, XrefRangeStart = 374, XrefRangeEnd = 376, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_Configuration_Public_Virtual_New_get_ScriptsConfiguration_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ScriptsConfiguration>(intPtr) : null;
		}
	}

	public unsafe virtual IResourceLoader<Script> ScriptLoader
	{
		[CallerCount(6)]
		[CachedScanResults(RefRangeStart = 777, RefRangeEnd = 783, XrefRangeStart = 777, XrefRangeEnd = 783, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<IResourceLoader<Script>>(intPtr) : null;
		}
	}

	public unsafe virtual IResourceLoader<Script> ExternalScriptLoader
	{
		[CallerCount(9)]
		[CachedScanResults(RefRangeStart = 9342, RefRangeEnd = 9351, XrefRangeStart = 9342, XrefRangeEnd = 9351, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<IResourceLoader<Script>>(intPtr) : null;
		}
	}

	public unsafe virtual int TotalCommandsCount
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_TotalCommandsCount_Public_Virtual_New_get_Int32_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(int*)IL2CPP.il2cpp_object_unbox(intPtr);
		}
		[CallerCount(0)]
		set
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = (nint)(&value);
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_set_TotalCommandsCount_Private_set_Void_Int32_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}
	}

	static ScriptManager()
	{
		Il2CppClassPointerStore<ScriptManager>.NativeClassPtr = IL2CPP.GetIl2CppClass("Elringus.Naninovel.Runtime.dll", "Naninovel", "ScriptManager");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr);
		NativeFieldInfoPtr__Configuration_k__BackingField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, "<Configuration>k__BackingField");
		NativeFieldInfoPtr__TotalCommandsCount_k__BackingField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, "<TotalCommandsCount>k__BackingField");
		NativeFieldInfoPtr_providers = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, "providers");
		NativeFieldInfoPtr_scriptLoader = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, "scriptLoader");
		NativeFieldInfoPtr_extScriptLoader = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, "extScriptLoader");
		NativeMethodInfoPtr_get_Configuration_Public_Virtual_New_get_ScriptsConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668488);
		NativeMethodInfoPtr_get_ScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668489);
		NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Virtual_Final_New_get_IResourceLoader_1_Script_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668490);
		NativeMethodInfoPtr_get_TotalCommandsCount_Public_Virtual_New_get_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668491);
		NativeMethodInfoPtr_set_TotalCommandsCount_Private_set_Void_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668492);
		NativeMethodInfoPtr__ctor_Public_Void_ScriptsConfiguration_IResourceProviderManager_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668493);
		NativeMethodInfoPtr_InitializeService_Public_Virtual_New_UniTask_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668494);
		NativeMethodInfoPtr_ResetService_Public_Virtual_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668495);
		NativeMethodInfoPtr_DestroyService_Public_Virtual_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr, 100668496);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 147339, RefRangeEnd = 147340, XrefRangeStart = 147339, XrefRangeEnd = 147339, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe ScriptManager(ScriptsConfiguration config, IResourceProviderManager providers)
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<ScriptManager>.NativeClassPtr))
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)config);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)providers);
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_ScriptsConfiguration_IResourceProviderManager_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 147340, XrefRangeEnd = 147356, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual UniTask InitializeService()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr);
		System.IntPtr pointer = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_InitializeService_Public_Virtual_New_UniTask_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr);
		Il2CppException.RaiseExceptionIfNecessary(intPtr);
		return new UniTask(pointer);
	}

	[CallerCount(0)]
	public unsafe virtual void ResetService()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_ResetService_Public_Virtual_New_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	public unsafe virtual void DestroyService()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_DestroyService_Public_Virtual_New_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public ScriptManager(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
