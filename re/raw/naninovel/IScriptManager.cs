using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;

namespace Naninovel;

public class IScriptManager : Il2CppObjectBase
{
	private static readonly IntPtr NativeMethodInfoPtr_get_ScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0;

	private static readonly IntPtr NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0;

	private static readonly IntPtr NativeMethodInfoPtr_get_TotalCommandsCount_Public_Abstract_Virtual_New_get_Int32_0;

	public unsafe virtual IResourceLoader<Script> ScriptLoader
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_ScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IResourceLoader<Script>>(intPtr) : null;
		}
	}

	public unsafe virtual IResourceLoader<Script> ExternalScriptLoader
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IResourceLoader<Script>>(intPtr) : null;
		}
	}

	public unsafe virtual int TotalCommandsCount
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_TotalCommandsCount_Public_Abstract_Virtual_New_get_Int32_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(int*)IL2CPP.il2cpp_object_unbox(intPtr);
		}
	}

	static IScriptManager()
	{
		Il2CppClassPointerStore<IScriptManager>.NativeClassPtr = IL2CPP.GetIl2CppClass("Elringus.Naninovel.Runtime.dll", "Naninovel", "IScriptManager");
		NativeMethodInfoPtr_get_ScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IScriptManager>.NativeClassPtr, 100668088);
		NativeMethodInfoPtr_get_ExternalScriptLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_Script_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IScriptManager>.NativeClassPtr, 100668089);
		NativeMethodInfoPtr_get_TotalCommandsCount_Public_Abstract_Virtual_New_get_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IScriptManager>.NativeClassPtr, 100668090);
	}

	public IScriptManager(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
