#include "StdAfx.h"

#include "AlignedObjectArray.h"
#include "CollisionAlgorithm.h"
#include "CollisionObject.h"
#include "CollisionObjectWrapper.h"
#include "Dispatcher.h"
#include "ManifoldResult.h"
#include "PersistentManifold.h"

CollisionAlgorithmConstructionInfo::~CollisionAlgorithmConstructionInfo()
{
	this->!CollisionAlgorithmConstructionInfo();
}

CollisionAlgorithmConstructionInfo::!CollisionAlgorithmConstructionInfo()
{
	delete _native;
	_native = NULL;
}

CollisionAlgorithmConstructionInfo::CollisionAlgorithmConstructionInfo()
{
	_native = new btCollisionAlgorithmConstructionInfo();
}

CollisionAlgorithmConstructionInfo::CollisionAlgorithmConstructionInfo(BulletSharp::Dispatcher^ dispatcher,
	int temp)
{
	_dispatcher = dispatcher;
	_native = new btCollisionAlgorithmConstructionInfo(dispatcher->_native, temp);
}

BulletSharp::Dispatcher^ CollisionAlgorithmConstructionInfo::Dispatcher::get()
{
	return _dispatcher;
}
void CollisionAlgorithmConstructionInfo::Dispatcher::set(BulletSharp::Dispatcher^ value)
{
	_dispatcher = value;
	_native->m_dispatcher1 = GetUnmanagedNullable(value);
}

PersistentManifold^ CollisionAlgorithmConstructionInfo::Manifold::get()
{
	return gcnew PersistentManifold(_native->m_manifold);
}
void CollisionAlgorithmConstructionInfo::Manifold::set(PersistentManifold^ value)
{
	_native->m_manifold = (btPersistentManifold*)value->_native;
}


CollisionAlgorithm::CollisionAlgorithm(btCollisionAlgorithm* native)
{
	_native = native;
}

CollisionAlgorithm::~CollisionAlgorithm()
{
	this->!CollisionAlgorithm();
}

CollisionAlgorithm::!CollisionAlgorithm()
{
	if (this->IsDisposed)
		return;

	OnDisposing(this, nullptr);

	delete _native;
	_native = NULL;

	OnDisposed(this, nullptr);
}

btScalar CollisionAlgorithm::CalculateTimeOfImpact(CollisionObject^ body0, CollisionObject^ body1,
	DispatcherInfo^ dispatchInfo, ManifoldResult^ resultOut)
{
	return _native->calculateTimeOfImpact(body0->_native, body1->_native, *dispatchInfo->_native,
		(btManifoldResult*)resultOut->_native);
}

void CollisionAlgorithm::GetAllContactManifolds(AlignedManifoldArray^ manifoldArray)
{
	_native->getAllContactManifolds(*(btManifoldArray*)manifoldArray->_native);
}

void CollisionAlgorithm::ProcessCollision(CollisionObjectWrapper^ body0Wrap, CollisionObjectWrapper^ body1Wrap,
	DispatcherInfo^ dispatchInfo, ManifoldResult^ resultOut)
{
	_native->processCollision(body0Wrap->_native, body1Wrap->_native, *dispatchInfo->_native,
		(btManifoldResult*)resultOut->_native);
}

bool CollisionAlgorithm::IsDisposed::get()
{
	return (_native == NULL);
}

