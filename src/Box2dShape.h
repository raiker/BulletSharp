#pragma once

#include "PolyhedralConvexShape.h"

namespace BulletSharp
{
	ref class Vector3Array;

	public ref class Box2dShape : PolyhedralConvexShape
	{
	private:
		Vector3Array^ _normals;
		Vector3Array^ _vertices;

	internal:
		Box2dShape(btBox2dShape* native);

	public:
		Box2dShape(Vector3 boxHalfExtents);
		Box2dShape(btScalar boxHalfExtentsX, btScalar boxHalfExtentsY, btScalar boxHalfExtentsZ);
		Box2dShape(btScalar boxHalfExtent); // cube helper

		Vector4 GetPlaneEquation(int i);
		Vector3 GetVertex(int i);

		property Vector3 Centroid
		{
			Vector3 get();
		}

		property Vector3 HalfExtentsWithMargin
		{
			Vector3 get();
		}

		property Vector3 HalfExtentsWithoutMargin
		{
			Vector3 get();
		}

		property Vector3Array^ Normals
		{
			Vector3Array^ get();
		}

		property Vector3Array^ Vertices
		{
			Vector3Array^ get();
		}
	};
};
