#include "StdAfx.h"

#include "BroadphaseProxy.h"
#include "BroadphaseInterface.h"
#include "Dispatcher.h"
#include "OverlappingPairCache.h"

BroadphaseAabbCallback::BroadphaseAabbCallback(btBroadphaseAabbCallback* callback)
{
	_unmanaged = callback;
}

BroadphaseAabbCallback::BroadphaseAabbCallback()
{
	_unmanaged = new BroadphaseAabbCallbackWrapper(this);
}

BroadphaseAabbCallback::~BroadphaseAabbCallback()
{
	this->!BroadphaseAabbCallback();
}

BroadphaseAabbCallback::!BroadphaseAabbCallback()
{
	delete _unmanaged;
	_unmanaged = 0;
}

btBroadphaseAabbCallback* BroadphaseAabbCallback::UnmanagedPointer::get()
{
	return _unmanaged;
}
void BroadphaseAabbCallback::UnmanagedPointer::set(btBroadphaseAabbCallback* value)
{
	_unmanaged = value;
}


BroadphaseAabbCallbackWrapper::BroadphaseAabbCallbackWrapper(BroadphaseAabbCallback^ aabbCallback)
{
	_aabbCallback = aabbCallback;
}

bool BroadphaseAabbCallbackWrapper::process(const btBroadphaseProxy* proxy)
{
	return _aabbCallback->Process(BroadphaseProxy::GetManaged((btBroadphaseProxy*)proxy));
}


BroadphaseRayCallback::BroadphaseRayCallback(btBroadphaseRayCallback* callback)
: BroadphaseAabbCallback(callback)
{
}

btBroadphaseRayCallback* BroadphaseRayCallback::UnmanagedPointer::get()
{
	return (btBroadphaseRayCallback*)BroadphaseAabbCallback::UnmanagedPointer;;
}

BroadphaseInterface::BroadphaseInterface(btBroadphaseInterface* broadphase)
{
	_broadphase = broadphase;
}

BroadphaseInterface::~BroadphaseInterface()
{
	this->!BroadphaseInterface();
}

BroadphaseInterface::!BroadphaseInterface()
{
	if (this->IsDisposed)
		return;

	OnDisposing(this, nullptr);

	_broadphase = NULL;

	OnDisposed(this, nullptr);
}

bool BroadphaseInterface::IsDisposed::get()
{
	return (_broadphase == NULL);
}

void BroadphaseInterface::AabbTest(Vector3 aabbMin, Vector3 aabbMax, BroadphaseAabbCallback^ callback)
{
	btVector3* aabbMinTemp = Math::Vector3ToBtVector3(aabbMin);
	btVector3* aabbMaxTemp = Math::Vector3ToBtVector3(aabbMax);

	_broadphase->aabbTest(*aabbMinTemp, *aabbMaxTemp, *callback->UnmanagedPointer);

	delete aabbMinTemp;
	delete aabbMaxTemp;
}

void BroadphaseInterface::CalculateOverlappingPairs(Dispatcher^ dispatcher)
{
	_broadphase->calculateOverlappingPairs(dispatcher->UnmanagedPointer);
}

BroadphaseProxy^ BroadphaseInterface::CreateProxy(Vector3 aabbMin, Vector3 aabbMax,
	int shapeType, IntPtr userPtr, CollisionFilterGroups collisionFilterGroup,
	CollisionFilterGroups collisionFilterMask, Dispatcher^ dispatcher, IntPtr multiSapProxy)
{
	btBroadphaseProxy* proxy = new btBroadphaseProxy;

	btVector3* aabbMinTemp = Math::Vector3ToBtVector3(aabbMin);
	btVector3* aabbMaxTemp = Math::Vector3ToBtVector3(aabbMax);

	proxy = _broadphase->createProxy(*aabbMinTemp, *aabbMaxTemp,
		shapeType, userPtr.ToPointer(), (short int)collisionFilterGroup, (short int)collisionFilterMask,
		dispatcher->UnmanagedPointer, multiSapProxy.ToPointer());

	delete aabbMinTemp;
	delete aabbMaxTemp;

	return BroadphaseProxy::GetManaged(proxy);
}

void BroadphaseInterface::DestroyProxy(BroadphaseProxy^ proxy, Dispatcher^ dispatcher)
{
	_broadphase->destroyProxy(proxy->UnmanagedPointer, dispatcher->UnmanagedPointer);
}

void BroadphaseInterface::GetAabb(BroadphaseProxy^ proxy, Vector3% aabbMin, Vector3% aabbMax)
{
	btVector3* aabbMinTemp = new btVector3;
	btVector3* aabbMaxTemp = new btVector3;

	_broadphase->getAabb(proxy->UnmanagedPointer, *aabbMinTemp, *aabbMaxTemp);

	Math::BtVector3ToVector3(aabbMinTemp, aabbMin);
	Math::BtVector3ToVector3(aabbMaxTemp, aabbMax);

	delete aabbMinTemp;
	delete aabbMaxTemp;
}

void BroadphaseInterface::GetBroadphaseAabb(Vector3% aabbMin, Vector3% aabbMax)
{
	btVector3* aabbMinTemp = new btVector3;
	btVector3* aabbMaxTemp = new btVector3;

	_broadphase->getBroadphaseAabb(*aabbMinTemp, *aabbMaxTemp);

	Math::BtVector3ToVector3(aabbMinTemp, aabbMin);
	Math::BtVector3ToVector3(aabbMaxTemp, aabbMax);

	delete aabbMinTemp;
	delete aabbMaxTemp;
}

void BroadphaseInterface::PrintStats()
{
	_broadphase->printStats();
}

#pragma managed(push, off)
void BroadphaseInterface_RayTest(btBroadphaseInterface* broadphase, btVector3* rayFrom, btVector3* rayTo,
	btBroadphaseRayCallback* rayCallback, btVector3* aabbMin)
{
	broadphase->rayTest(*rayFrom, *rayTo, *rayCallback, *aabbMin);
}
void BroadphaseInterface_RayTest(btBroadphaseInterface* broadphase, btVector3* rayFrom, btVector3* rayTo,
	btBroadphaseRayCallback* rayCallback)
{
	broadphase->rayTest(*rayFrom, *rayTo, *rayCallback);
}
#pragma managed(pop)

void BroadphaseInterface::RayTest(Vector3 rayFrom, Vector3 rayTo, BroadphaseRayCallback^ rayCallback,
								  Vector3 aabbMin, Vector3 aabbMax)
{
	btVector3* rayFromTemp = Math::Vector3ToBtVector3(rayFrom);
	btVector3* rayToTemp = Math::Vector3ToBtVector3(rayTo);
	btVector3* aabbMinTemp = Math::Vector3ToBtVector3(aabbMin);
	btVector3* aabbMaxTemp = Math::Vector3ToBtVector3(aabbMax);

	_broadphase->rayTest(*rayFromTemp, *rayToTemp, *rayCallback->UnmanagedPointer, *aabbMinTemp, *aabbMaxTemp);

	delete rayFromTemp;
	delete rayToTemp;
	delete aabbMinTemp;
	delete aabbMaxTemp;
}

void BroadphaseInterface::RayTest(Vector3 rayFrom, Vector3 rayTo, BroadphaseRayCallback^ rayCallback, Vector3 aabbMin)
{
	btVector3* rayFromTemp = Math::Vector3ToBtVector3(rayFrom);
	btVector3* rayToTemp = Math::Vector3ToBtVector3(rayTo);
	btVector3* aabbMinTemp = Math::Vector3ToBtVector3(aabbMin);

	BroadphaseInterface_RayTest(_broadphase, rayFromTemp, rayToTemp, rayCallback->UnmanagedPointer, aabbMinTemp);

	delete rayFromTemp;
	delete rayToTemp;
	delete aabbMinTemp;
}

void BroadphaseInterface::RayTest(Vector3 rayFrom, Vector3 rayTo, BroadphaseRayCallback^ rayCallback)
{
	btVector3* rayFromTemp = Math::Vector3ToBtVector3(rayFrom);
	btVector3* rayToTemp = Math::Vector3ToBtVector3(rayTo);

	BroadphaseInterface_RayTest(_broadphase, rayFromTemp, rayToTemp, rayCallback->UnmanagedPointer);

	delete rayFromTemp;
	delete rayToTemp;
}

void BroadphaseInterface::ResetPool(Dispatcher^ dispatcher)
{
	_broadphase->resetPool(dispatcher->UnmanagedPointer);
}

void BroadphaseInterface::SetAabb(BroadphaseProxy^ proxy, Vector3 aabbMin, Vector3 aabbMax, Dispatcher^ dispatcher)
{
	btVector3* aabbMinTemp = Math::Vector3ToBtVector3(aabbMin);
	btVector3* aabbMaxTemp = Math::Vector3ToBtVector3(aabbMax);

	_broadphase->setAabb(proxy->UnmanagedPointer, *aabbMinTemp, *aabbMaxTemp, dispatcher->UnmanagedPointer);

	delete aabbMinTemp;
	delete aabbMaxTemp;
}

OverlappingPairCache^ BroadphaseInterface::OverlappingPairCache::get()
{
	return dynamic_cast<BulletSharp::OverlappingPairCache^>(
		BulletSharp::OverlappingPairCache::GetManaged(_broadphase->getOverlappingPairCache()));
}

btBroadphaseInterface* BroadphaseInterface::UnmanagedPointer::get()
{
	return _broadphase;
}
void BroadphaseInterface::UnmanagedPointer::set(btBroadphaseInterface* value)
{
	_broadphase = value;
}
