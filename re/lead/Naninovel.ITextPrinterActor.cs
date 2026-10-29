using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Collections.Generic;

namespace Naninovel;

public class ITextPrinterActor : Il2CppObjectBase
{
	private static readonly IntPtr NativeMethodInfoPtr_get_Messages_Public_Abstract_Virtual_New_get_IReadOnlyList_1_PrintedMessage_0;

	private static readonly IntPtr NativeMethodInfoPtr_set_Messages_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_PrintedMessage_0;

	private static readonly IntPtr NativeMethodInfoPtr_get_Templates_Public_Abstract_Virtual_New_get_IReadOnlyList_1_MessageTemplate_0;

	private static readonly IntPtr NativeMethodInfoPtr_set_Templates_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_MessageTemplate_0;

	private static readonly IntPtr NativeMethodInfoPtr_get_RevealProgress_Public_Abstract_Virtual_New_get_Single_0;

	private static readonly IntPtr NativeMethodInfoPtr_set_RevealProgress_Public_Abstract_Virtual_New_set_Void_Single_0;

	private static readonly IntPtr NativeMethodInfoPtr_AddMessage_Public_Abstract_Virtual_New_Void_PrintedMessage_0;

	private static readonly IntPtr NativeMethodInfoPtr_AppendText_Public_Abstract_Virtual_New_Void_LocalizableText_0;

	private static readonly IntPtr NativeMethodInfoPtr_Reveal_Public_Abstract_Virtual_New_UniTask_Single_AsyncToken_0;

	public unsafe virtual IReadOnlyList<PrintedMessage> Messages
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_Messages_Public_Abstract_Virtual_New_get_IReadOnlyList_1_PrintedMessage_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IReadOnlyList<PrintedMessage>>(intPtr) : null;
		}
		[CallerCount(0)]
		set
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = stackalloc IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)value);
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_set_Messages_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_PrintedMessage_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}
	}

	public unsafe virtual IReadOnlyList<MessageTemplate> Templates
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_Templates_Public_Abstract_Virtual_New_get_IReadOnlyList_1_MessageTemplate_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IReadOnlyList<MessageTemplate>>(intPtr) : null;
		}
		[CallerCount(0)]
		set
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = stackalloc IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)value);
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_set_Templates_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_MessageTemplate_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}
	}

	public unsafe virtual float RevealProgress
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_RevealProgress_Public_Abstract_Virtual_New_get_Single_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(float*)IL2CPP.il2cpp_object_unbox(intPtr);
		}
		[CallerCount(0)]
		set
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = stackalloc IntPtr[1];
			*ptr = (nint)(&value);
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_set_RevealProgress_Public_Abstract_Virtual_New_set_Void_Single_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}
	}

	static ITextPrinterActor()
	{
		Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr = IL2CPP.GetIl2CppClass("Elringus.Naninovel.Runtime.dll", "Naninovel", "ITextPrinterActor");
		NativeMethodInfoPtr_get_Messages_Public_Abstract_Virtual_New_get_IReadOnlyList_1_PrintedMessage_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663728);
		NativeMethodInfoPtr_set_Messages_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_PrintedMessage_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663729);
		NativeMethodInfoPtr_get_Templates_Public_Abstract_Virtual_New_get_IReadOnlyList_1_MessageTemplate_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663730);
		NativeMethodInfoPtr_set_Templates_Public_Abstract_Virtual_New_set_Void_IReadOnlyList_1_MessageTemplate_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663731);
		NativeMethodInfoPtr_get_RevealProgress_Public_Abstract_Virtual_New_get_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663732);
		NativeMethodInfoPtr_set_RevealProgress_Public_Abstract_Virtual_New_set_Void_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663733);
		NativeMethodInfoPtr_AddMessage_Public_Abstract_Virtual_New_Void_PrintedMessage_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663734);
		NativeMethodInfoPtr_AppendText_Public_Abstract_Virtual_New_Void_LocalizableText_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663735);
		NativeMethodInfoPtr_Reveal_Public_Abstract_Virtual_New_UniTask_Single_AsyncToken_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ITextPrinterActor>.NativeClassPtr, 100663736);
	}

	[CallerCount(0)]
	public unsafe virtual void AddMessage(PrintedMessage message)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)message));
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_AddMessage_Public_Abstract_Virtual_New_Void_PrintedMessage_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	public unsafe virtual void AppendText(LocalizableText text)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)text));
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_AppendText_Public_Abstract_Virtual_New_Void_LocalizableText_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	public unsafe virtual UniTask Reveal(float delay, AsyncToken token = null)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[2];
		*ptr = (nint)(&delay);
		*(IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)token));
		Unsafe.SkipInit(out IntPtr intPtr);
		IntPtr pointer = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_Reveal_Public_Abstract_Virtual_New_UniTask_Single_AsyncToken_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr);
		Il2CppException.RaiseExceptionIfNecessary(intPtr);
		return new UniTask(pointer);
	}

	public ITextPrinterActor(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
