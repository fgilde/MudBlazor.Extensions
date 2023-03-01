package mono.com.google.android.material.internal;


public class MaterialCheckable_OnCheckedChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.material.internal.MaterialCheckable.OnCheckedChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCheckedChanged:(Ljava/lang/Object;Z)V:GetOnCheckedChanged_Ljava_lang_Object_ZHandler:Google.Android.Material.Internal.IMaterialCheckableOnCheckedChangeListenerInvoker, Xamarin.Google.Android.Material\n" +
			"";
		mono.android.Runtime.register ("Google.Android.Material.Internal.IMaterialCheckableOnCheckedChangeListenerImplementor, Xamarin.Google.Android.Material", MaterialCheckable_OnCheckedChangeListenerImplementor.class, __md_methods);
	}


	public MaterialCheckable_OnCheckedChangeListenerImplementor ()
	{
		super ();
		if (getClass () == MaterialCheckable_OnCheckedChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("Google.Android.Material.Internal.IMaterialCheckableOnCheckedChangeListenerImplementor, Xamarin.Google.Android.Material", "", this, new java.lang.Object[] {  });
		}
	}


	public void onCheckedChanged (java.lang.Object p0, boolean p1)
	{
		n_onCheckedChanged (p0, p1);
	}

	private native void n_onCheckedChanged (java.lang.Object p0, boolean p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
