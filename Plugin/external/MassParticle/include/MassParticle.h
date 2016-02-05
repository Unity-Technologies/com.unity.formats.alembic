#ifndef MassParticle_h
#define MassParticle_h

#define mpCLinkage extern "C"
#ifdef mpImpl
    #ifndef mpStaticLink
        #define mpAPI __declspec(dllexport)
    #else
        #define mpAPI 
    #endif
#else  // mpImpl
    #ifndef mpStaticLink
        #define mpAPI __declspec(dllimport)
        #pragma comment(lib, "MassParticle.lib")
    #else
        #define mpAPI 
        #pragma comment(lib, "MassParticle_s.lib")
    #endif
#endif // mpImpl



enum mpSolverType
{
    mpSolverType_Impulse,
    mpSolverType_SPH,
    mpSolverType_SPHEst,
};

enum mpForceShape
{
    mpForceShape_AffectAll,
    mpForceShape_Sphere,
    mpForceShape_Capsule,
    mpForceShape_Box,
};

enum mpForceType
{
    mpForceType_Directional,
    mpForceType_Radial,
    mpForceType_RadialCapsule,
    mpForceType_Vortex,
    mpForceType_Spline,
    mpForceType_VectorField,
};

#ifdef mpImpl
    typedef vec3    mpV3;
    typedef ivec3   mpV3i;
    typedef mat4    mpM44;
#else
    #include <cstdint>
    template<class T> struct mpV3T
    {
        T x, y, z;
        mpV3T() : x(), y(), z() {}
        mpV3T(T x_, T y_, T z_) : x(x_), y(y_), z(z_) {}
    };
    typedef mpV3T<float> mpV3;
    typedef mpV3T<int> mpV3i;
    typedef mpV3 mpV3;
    struct mpM44 { float v[16]; };

    struct mpKernelParams
    {
        mpV3 world_center;
        mpV3 world_extent;
        mpV3i world_div;
        mpV3 active_region_center;  // if particle is outside of this region, it will die immediately.
        mpV3 active_region_extent;  // if both of this is not assigned, it will be treated as world_center & world_extent.
        mpV3 coord_scaler;
        mpSolverType solver_type;
        int32_t enable_interaction;
        int32_t enable_colliders;
        int32_t enable_forces;
        int32_t id_as_float;
        float timestep;
        float damping;
        float advection;
        float pressure_stiffness;
        int32_t max_particles;
        float particle_size;
        float SPHRestDensity;
        float SPHParticleMass;
        float SPHViscosity;
        float reserved[4];

        mpKernelParams()
        {
            world_center = mpV3(0.0f, 0.0f, 0.0f);
            world_extent = mpV3(10.24f, 10.24f, 10.24f);
            world_div    = mpV3i(128, 128, 128);
            coord_scaler = mpV3(1.0f, 1.0f, 1.0f);

            solver_type = mpSolverType_Impulse;
            enable_interaction = 1;
            enable_colliders = 1;
            enable_forces = 1;
            id_as_float = 0;

            timestep = 1.0f / 60.0f;
            damping = 0.6f;
            advection = 0.5f;
            pressure_stiffness = 500.0f;

            max_particles = 100000;
            particle_size = 0.08f;

            SPHRestDensity = 1000.0f;
            SPHParticleMass = 0.002f;
            SPHViscosity = 0.1f;
        }

    };

    struct mpParticle
    {
        mpV3 position;
        uint32_t id;

        mpV3 velocity;
        float speed;

        uint32_t hash;
        float lifetime;
        uint16_t hit;
        uint16_t hit_prev;
        int userdata;
    };

    struct mpParticleIM
    {
        mpV3 accel;
        float pad;
    };

    struct mpParticleForce
    {
        int num_hits;
        int pad0[3];

        mpV3 position;
        int pad1;

        mpV3 force;
        int pad2;
    };

    typedef void(__stdcall *mpHitHandler)(mpParticle *p);
    typedef void(__stdcall *mpForceHandler)(mpParticleForce *p);

    struct mpSpawnParams
    {
        mpV3 velocity_base;
        float velocity_random_diffuse;
        float lifetime;
        float lifetime_random_diffuse;
        int userdata;
        mpHitHandler handler;
    };

    struct mpColliderProperties
    {
        int32_t owner_id;
        float stiffness;
        mpHitHandler hit_handler;
        mpForceHandler force_handler;
    };

    struct mpForceProperties
    {
        mpForceShape shape;
        mpForceType type;
        float strength_near;
        float strength_far;
        float range_inner;
        float range_outer;
        float rcp_range;
        float attenuation_exp;
        float random_seed;
        float random_diffuse;
        mpV3 direction;
        mpV3 center;
        mpV3 rcp_cellsize;
    };

#endif


mpCLinkage mpAPI void           mpUpdateDataTexture(int context, void *tex, int width, int height);

mpCLinkage mpAPI int            mpCreateContext();
mpCLinkage mpAPI void           mpDestroyContext(int context);
mpCLinkage mpAPI void           mpUpdate(int context, float dt);
mpCLinkage mpAPI void           mpBeginUpdate(int context, float dt);   // async version
mpCLinkage mpAPI void           mpEndUpdate(int context);               // 
mpCLinkage mpAPI void           mpCallHandlers(int context);

mpCLinkage mpAPI void           mpClearParticles(int context);
mpCLinkage mpAPI void           mpClearCollidersAndForces(int context);

mpCLinkage mpAPI void           mpGetKernelParams(int context, mpKernelParams *params);
mpCLinkage mpAPI void           mpSetKernelParams(int context, const mpKernelParams *params);

mpCLinkage mpAPI int            mpGetNumParticles(int context);
mpCLinkage mpAPI void           mpForceSetNumParticles(int context, int num);
mpCLinkage mpAPI mpParticleIM*  mpGetIntermediateData(int context, int nth=-1);
mpCLinkage mpAPI mpParticle*    mpGetParticles(int context);
mpCLinkage mpAPI void           mpAddParticles(int context, mpParticle *particles, int num_particles);
mpCLinkage mpAPI void           mpScatterParticlesSphere(int context, mpV3 *center, float radius, int num, const mpSpawnParams *params);
mpCLinkage mpAPI void           mpScatterParticlesBox(int context, mpV3 *center, mpV3 *size, int num, const mpSpawnParams *params);
mpCLinkage mpAPI void           mpScatterParticlesSphereTransform(int context, mpM44 *transform, int num, const mpSpawnParams *params);
mpCLinkage mpAPI void           mpScatterParticlesBoxTransform(int context, mpM44 *transform, int num, const mpSpawnParams *params);

mpCLinkage mpAPI void           mpAddSphereCollider(int context, mpColliderProperties *props, mpV3 *center, float radius);
mpCLinkage mpAPI void           mpAddCapsuleCollider(int context, mpColliderProperties *props, mpV3 *pos1, mpV3 *pos2, float radius);
mpCLinkage mpAPI void           mpAddBoxCollider(int context, mpColliderProperties *props, mpM44 *transform, mpV3 *center, mpV3 *size);
mpCLinkage mpAPI void           mpRemoveCollider(int context, mpColliderProperties *props);
mpCLinkage mpAPI void           mpAddForce(int context, mpForceProperties *p, mpM44 *trans);

mpCLinkage mpAPI void           mpScanSphere(int context, mpHitHandler handler, mpV3 *center, float radius);
mpCLinkage mpAPI void           mpScanAABB(int context, mpHitHandler handler, mpV3 *center, mpV3 *extent);
mpCLinkage mpAPI void           mpScanSphereParallel(int context, mpHitHandler handler, mpV3 *center, float radius);
mpCLinkage mpAPI void           mpScanAABBParallel(int context, mpHitHandler handler, mpV3 *center, mpV3 *extent);
mpCLinkage mpAPI void           mpScanAll(int context, mpHitHandler handler);
mpCLinkage mpAPI void           mpScanAllParallel(int context, mpHitHandler handler);

mpCLinkage mpAPI void           mpMoveAll(int context, mpV3 *move_amount);

// for static link usage. initialize graphics device manually.
void mpUnitySetGraphicsDevice(void* device, int deviceType, int eventType);

#endif // MassParticle_h
