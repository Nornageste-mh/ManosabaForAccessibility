using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using UniRx;

namespace GigaCreation.Tools.Debugging.Core;

public class DebugManager : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr__isDebugMode;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_IsDebugMode_Public_Virtual_Final_New_get_IReactiveProperty_1_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0;

	public unsafe ReactiveProperty<bool> _isDebugMode
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__isDebugMode);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ReactiveProperty<bool>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__isDebugMode)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)reactiveProperty));
		}
	}

	public unsafe virtual IReactiveProperty<bool> IsDebugMode
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 374, RefRangeEnd = 376, XrefRangeStart = 374, XrefRangeEnd = 376, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_IsDebugMode_Public_Virtual_Final_New_get_IReactiveProperty_1_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<IReactiveProperty<bool>>(intPtr) : null;
		}
	}

	static DebugManager()
	{
		Il2CppClassPointerStore<DebugManager>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.Tools.Debugging.Core.dll", "GigaCreation.Tools.Debugging.Core", "DebugManager");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DebugManager>.NativeClassPtr);
		NativeFieldInfoPtr__isDebugMode = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DebugManager>.NativeClassPtr, "_isDebugMode");
		NativeMethodInfoPtr_get_IsDebugMode_Public_Virtual_Final_New_get_IReactiveProperty_1_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebugManager>.NativeClassPtr, 100663299);
		NativeMethodInfoPtr__ctor_Public_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebugManager>.NativeClassPtr, 100663300);
		NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebugManager>.NativeClassPtr, 100663301);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 962980, XrefRangeEnd = 962986, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe DebugManager(bool initialMode)
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DebugManager>.NativeClassPtr))
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&initialMode);
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 962986, XrefRangeEnd = 962990, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual void Dispose()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public DebugManager(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
