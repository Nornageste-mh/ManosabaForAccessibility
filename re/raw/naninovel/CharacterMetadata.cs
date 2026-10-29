using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Reflection;
using UnityEngine;

namespace Naninovel;

[System.Serializable]
public class CharacterMetadata : OrthoActorMetadata
{
	[System.Serializable]
	public class Map : ActorMetadataMap<CharacterMetadata>
	{
		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

		static Map()
		{
			Il2CppClassPointerStore<Map>.NativeClassPtr = IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "Map");
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<Map>.NativeClassPtr);
			NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Map>.NativeClassPtr, 100663536);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 98729, XrefRangeEnd = 98732, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe Map()
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Map>.NativeClassPtr))
		{
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}

		public Map(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	[System.Serializable]
	public class Pose : ActorPose<CharacterState>
	{
		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

		static Pose()
		{
			Il2CppClassPointerStore<Pose>.NativeClassPtr = IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "Pose");
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<Pose>.NativeClassPtr);
			NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Pose>.NativeClassPtr, 100663537);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 98732, XrefRangeEnd = 98733, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe Pose()
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Pose>.NativeClassPtr))
		{
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}

		public Pose(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	[ObfuscatedName("Naninovel.CharacterMetadata+<>c__DisplayClass21_0`1")]
	public sealed class __c__DisplayClass21_0<TState> : Il2CppSystem.Object where TState : ActorState
	{
		private static readonly System.IntPtr NativeFieldInfoPtr_poseName;

		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

		private static readonly System.IntPtr NativeMethodInfoPtr__GetPose_b__0_Internal_Boolean_Pose_0;

		public unsafe string poseName
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_poseName);
				return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
			}
			set
			{
				System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
				IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_poseName)), IL2CPP.ManagedStringToIl2Cpp(text));
			}
		}

		static __c__DisplayClass21_0()
		{
			Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr = IL2CPP.il2cpp_class_from_type(Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "<>c__DisplayClass21_0`1"))).MakeGenericType(new Il2CppReferenceArray<Il2CppSystem.Type>(new Il2CppSystem.Type[1] { Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(Il2CppClassPointerStore<TState>.NativeClassPtr)) })).TypeHandle.value);
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr);
			NativeFieldInfoPtr_poseName = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr, "poseName");
			NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr, 100663538);
			NativeMethodInfoPtr__GetPose_b__0_Internal_Boolean_Pose_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr, 100663539);
		}

		[CallerCount(0)]
		public unsafe __c__DisplayClass21_0()
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<__c__DisplayClass21_0<TState>>.NativeClassPtr))
		{
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 98733, XrefRangeEnd = 98735, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe bool _GetPose_b__0(Pose p)
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)p);
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__GetPose_b__0_Internal_Boolean_Pose_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(bool*)IL2CPP.il2cpp_object_unbox(intPtr);
		}

		public __c__DisplayClass21_0(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	private sealed class MethodInfoStoreGeneric_GetPose_Public_Virtual_ActorPose_1_TState_String_0<TState>
	{
		internal static System.IntPtr Pointer = IL2CPP.il2cpp_method_get_from_reflection(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)new MethodInfo(IL2CPP.il2cpp_method_get_object(NativeMethodInfoPtr_GetPose_Public_Virtual_ActorPose_1_TState_String_0, Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr)).MakeGenericMethod(new Il2CppReferenceArray<Il2CppSystem.Type>(new Il2CppSystem.Type[1] { Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(Il2CppClassPointerStore<TState>.NativeClassPtr)) }))));
	}

	private static readonly System.IntPtr NativeFieldInfoPtr_BakedLookDirection;

	private static readonly System.IntPtr NativeFieldInfoPtr_HasName;

	private static readonly System.IntPtr NativeFieldInfoPtr_DisplayName;

	private static readonly System.IntPtr NativeFieldInfoPtr_UseCharacterColor;

	private static readonly System.IntPtr NativeFieldInfoPtr_NameColor;

	private static readonly System.IntPtr NativeFieldInfoPtr_MessageColor;

	private static readonly System.IntPtr NativeFieldInfoPtr_HighlightWhenSpeaking;

	private static readonly System.IntPtr NativeFieldInfoPtr_HighlightCharacterCount;

	private static readonly System.IntPtr NativeFieldInfoPtr_SpeakingPose;

	private static readonly System.IntPtr NativeFieldInfoPtr_NotSpeakingPose;

	private static readonly System.IntPtr NativeFieldInfoPtr_PlaceOnTop;

	private static readonly System.IntPtr NativeFieldInfoPtr_HighlightDuration;

	private static readonly System.IntPtr NativeFieldInfoPtr_HighlightEasing;

	private static readonly System.IntPtr NativeFieldInfoPtr_MessageSound;

	private static readonly System.IntPtr NativeFieldInfoPtr_MessageSoundPlayback;

	private static readonly System.IntPtr NativeFieldInfoPtr_VoiceSource;

	private static readonly System.IntPtr NativeFieldInfoPtr_LinkedPrinter;

	private static readonly System.IntPtr NativeFieldInfoPtr_Poses;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetPose_Public_Virtual_ActorPose_1_TState_String_0;

	public unsafe CharacterLookDirection BakedLookDirection
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BakedLookDirection);
			return *(CharacterLookDirection*)num;
		}
		set
		{
			*(CharacterLookDirection*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BakedLookDirection)) = characterLookDirection;
		}
	}

	public unsafe bool HasName
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HasName);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HasName)) = flag;
		}
	}

	public unsafe string DisplayName
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DisplayName);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DisplayName)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe bool UseCharacterColor
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_UseCharacterColor);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_UseCharacterColor)) = flag;
		}
	}

	public unsafe Color NameColor
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameColor);
			return *(Color*)num;
		}
		set
		{
			*(Color*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameColor)) = color;
		}
	}

	public unsafe Color MessageColor
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageColor);
			return *(Color*)num;
		}
		set
		{
			*(Color*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageColor)) = color;
		}
	}

	public unsafe bool HighlightWhenSpeaking
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightWhenSpeaking);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightWhenSpeaking)) = flag;
		}
	}

	public unsafe int HighlightCharacterCount
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightCharacterCount);
			return *(int*)num;
		}
		set
		{
			*(int*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightCharacterCount)) = num;
		}
	}

	public unsafe string SpeakingPose
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_SpeakingPose);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_SpeakingPose)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe string NotSpeakingPose
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NotSpeakingPose);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NotSpeakingPose)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe bool PlaceOnTop
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_PlaceOnTop);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_PlaceOnTop)) = flag;
		}
	}

	public unsafe float HighlightDuration
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightDuration);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightDuration)) = num;
		}
	}

	public unsafe EasingType HighlightEasing
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightEasing);
			return *(EasingType*)num;
		}
		set
		{
			*(EasingType*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HighlightEasing)) = easingType;
		}
	}

	public unsafe string MessageSound
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageSound);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageSound)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe MessageSoundPlayback MessageSoundPlayback
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageSoundPlayback);
			return *(MessageSoundPlayback*)num;
		}
		set
		{
			*(MessageSoundPlayback*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_MessageSoundPlayback)) = messageSoundPlayback;
		}
	}

	public unsafe AudioSource VoiceSource
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_VoiceSource);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<AudioSource>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_VoiceSource)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)audioSource));
		}
	}

	public unsafe string LinkedPrinter
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_LinkedPrinter);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_LinkedPrinter)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe List<Pose> Poses
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Poses);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Pose>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Poses)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)list));
		}
	}

	static CharacterMetadata()
	{
		Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr = IL2CPP.GetIl2CppClass("Elringus.Naninovel.Runtime.dll", "Naninovel", "CharacterMetadata");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr);
		NativeFieldInfoPtr_BakedLookDirection = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "BakedLookDirection");
		NativeFieldInfoPtr_HasName = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "HasName");
		NativeFieldInfoPtr_DisplayName = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "DisplayName");
		NativeFieldInfoPtr_UseCharacterColor = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "UseCharacterColor");
		NativeFieldInfoPtr_NameColor = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "NameColor");
		NativeFieldInfoPtr_MessageColor = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "MessageColor");
		NativeFieldInfoPtr_HighlightWhenSpeaking = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "HighlightWhenSpeaking");
		NativeFieldInfoPtr_HighlightCharacterCount = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "HighlightCharacterCount");
		NativeFieldInfoPtr_SpeakingPose = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "SpeakingPose");
		NativeFieldInfoPtr_NotSpeakingPose = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "NotSpeakingPose");
		NativeFieldInfoPtr_PlaceOnTop = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "PlaceOnTop");
		NativeFieldInfoPtr_HighlightDuration = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "HighlightDuration");
		NativeFieldInfoPtr_HighlightEasing = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "HighlightEasing");
		NativeFieldInfoPtr_MessageSound = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "MessageSound");
		NativeFieldInfoPtr_MessageSoundPlayback = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "MessageSoundPlayback");
		NativeFieldInfoPtr_VoiceSource = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "VoiceSource");
		NativeFieldInfoPtr_LinkedPrinter = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "LinkedPrinter");
		NativeFieldInfoPtr_Poses = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, "Poses");
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, 100663534);
		NativeMethodInfoPtr_GetPose_Public_Virtual_ActorPose_1_TState_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr, 100663535);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 98756, RefRangeEnd = 98757, XrefRangeStart = 98735, XrefRangeEnd = 98756, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe CharacterMetadata()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<CharacterMetadata>.NativeClassPtr))
	{
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 98757, XrefRangeEnd = 98768, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override ActorPose<TState> GetPose<TState>(string poseName)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(poseName);
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), MethodInfoStoreGeneric_GetPose_Public_Virtual_ActorPose_1_TState_String_0<TState>.Pointer), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ActorPose<TState>>(intPtr) : null;
	}

	public CharacterMetadata(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
