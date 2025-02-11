## External Components
External components which are not included in the base version of PA are dynamically copied in this directory at build time.
- If external components exist (currently, the only one is `PA.SelfReflection`), they are copied into `dyn` at build time.
- If external components are  missing, its `stub` is copied into `dyn` instead, and PA builds without the additional logic/functionality.

For more information on the import resolution, have a look at at [vite config](../../vite.config.ts) and watch out for `@external` and `@externalVue` imports throughout the codebase.