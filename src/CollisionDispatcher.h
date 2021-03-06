#pragma once

#include "Dispatcher.h"

namespace BulletSharp
{
	ref class CollisionAlgorithmCreateFunc;
	ref class CollisionConfiguration;

	public delegate void NearCallback(BroadphasePair^ collisionPair,
		CollisionDispatcher^ dispatcher, DispatcherInfo^ dispatchInfo);

	class btCollisionDispatcherWrapper : public btCollisionDispatcher
	{
	public:
		void* _nearCallback;

	public:
		btCollisionDispatcherWrapper (btCollisionConfiguration* collisionConfiguration);
	};

	class NearCallbackWrapper
	{
	public:
		static void nearCallback (btBroadphasePair& collisionPair, btCollisionDispatcherWrapper& dispatcher, const btDispatcherInfo& dispatchInfo);
	};

	public ref class CollisionDispatcher : Dispatcher
	{
	protected:
		CollisionConfiguration^ _collisionConfiguration;

	private:
		btNearCallback originalCallback;
		System::Collections::Generic::List<CollisionAlgorithmCreateFunc^>^ _collisionCreateFuncs;

	internal:
		CollisionDispatcher(btCollisionDispatcher* native);

	public:
		CollisionDispatcher(CollisionConfiguration^ collisionConfiguration);
		CollisionDispatcher();

		static void DefaultNearCallback(BroadphasePair^ collisionPair,
			CollisionDispatcher^ dispatcher, DispatcherInfo^ dispatchInfo);

		void RegisterCollisionCreateFunc(BroadphaseNativeType proxyType0,
			BroadphaseNativeType proxyType1, CollisionAlgorithmCreateFunc^ createFunc);

		property CollisionConfiguration^ CollisionConfiguration
		{
			BulletSharp::CollisionConfiguration^ get();
			void set(BulletSharp::CollisionConfiguration^ value);
		}

		property BulletSharp::DispatcherFlags DispatcherFlags
		{
			BulletSharp::DispatcherFlags get();
			void set(BulletSharp::DispatcherFlags value);
		}

		property BulletSharp::NearCallback^ NearCallback
		{
			BulletSharp::NearCallback^ get();
			void set(BulletSharp::NearCallback^ value);
		}
	};
};
