#include "StdAfx.h"

#include "CylinderShape.h"

#define Native static_cast<btCylinderShape*>(_native)

CylinderShape::CylinderShape(btCylinderShape* native)
	: ConvexInternalShape(native)
{
}

CylinderShape::CylinderShape(Vector3 halfExtents)
	: ConvexInternalShape(0)
{
	VECTOR3_DEF(halfExtents);
	UnmanagedPointer = new btCylinderShape(VECTOR3_USE(halfExtents));
	VECTOR3_DEL(halfExtents);
}

CylinderShape::CylinderShape(btScalar halfExtentsX, btScalar halfExtentsY, btScalar halfExtentsZ)
	: ConvexInternalShape(0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtentsX,halfExtentsY,halfExtentsZ);
	UnmanagedPointer = new btCylinderShape(*halfExtentsTemp);
	delete halfExtentsTemp;
}

CylinderShape::CylinderShape(btScalar halfExtents)
	: ConvexInternalShape(0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtents,halfExtents,halfExtents);
	UnmanagedPointer = new btCylinderShape(*halfExtentsTemp);
	delete halfExtentsTemp;
}

Vector3 CylinderShape::HalfExtentsWithMargin::get()
{
	btVector3* extentsTemp = new btVector3(Native->getHalfExtentsWithMargin());
	Vector3 extents = Math::BtVector3ToVector3(extentsTemp);
	delete extentsTemp;
	return extents;
}

Vector3 CylinderShape::HalfExtentsWithoutMargin::get()
{
	return Math::BtVector3ToVector3(&Native->getHalfExtentsWithoutMargin());
}

btScalar CylinderShape::Radius::get()
{
	return Native->getRadius();
}

int CylinderShape::UpAxis::get()
{
	return Native->getUpAxis();
}


CylinderShapeX::CylinderShapeX(Vector3 halfExtents)
	: CylinderShape((btCylinderShape*)0)
{
	VECTOR3_DEF(halfExtents);
	UnmanagedPointer = new btCylinderShapeX(VECTOR3_USE(halfExtents));
	VECTOR3_DEL(halfExtents);
}

CylinderShapeX::CylinderShapeX(btScalar halfExtentsX, btScalar halfExtentsY, btScalar halfExtentsZ)
	: CylinderShape((btCylinderShape*)0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtentsX,halfExtentsY,halfExtentsZ);
	UnmanagedPointer = new btCylinderShapeX(*halfExtentsTemp);
	delete halfExtentsTemp;
}

CylinderShapeX::CylinderShapeX(btScalar halfExtents)
	: CylinderShape((btCylinderShape*)0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtents,halfExtents,halfExtents);
	UnmanagedPointer = new btCylinderShapeX(*halfExtentsTemp);
	delete halfExtentsTemp;
}


CylinderShapeZ::CylinderShapeZ(Vector3 halfExtents)
	: CylinderShape((btCylinderShape*)0)
{
	VECTOR3_DEF(halfExtents);
	UnmanagedPointer = new btCylinderShapeZ(VECTOR3_USE(halfExtents));
	VECTOR3_DEL(halfExtents);
}

CylinderShapeZ::CylinderShapeZ(btScalar halfExtentsX, btScalar halfExtentsY, btScalar halfExtentsZ)
: CylinderShape((btCylinderShape*)0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtentsX,halfExtentsY,halfExtentsZ);
	UnmanagedPointer = new btCylinderShapeZ(*halfExtentsTemp);
	delete halfExtentsTemp;
}

CylinderShapeZ::CylinderShapeZ(btScalar halfExtents)
	: CylinderShape((btCylinderShape*)0)
{
	btVector3* halfExtentsTemp = new btVector3(halfExtents,halfExtents,halfExtents);
	UnmanagedPointer = new btCylinderShapeZ(*halfExtentsTemp);
	delete halfExtentsTemp;
}