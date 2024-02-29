import { createRouter, createWebHashHistory, Router } from 'vue-router';

const router: Router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/about',
      name: 'About',
      component: () => import('../views/AboutView.vue')
    },
    {
      path: '/experience-sampling',
      name: 'ExperienceSampling',
      component: () => import('../views/ExperienceSamplingView.vue')
    },
    {
      path: '/onboarding',
      name: 'Onboarding',
      component: () => import('../views/OnboardingView.vue'),
      props: (route) => ({ query: route.query.isMacOS })
    },
    {
      path: '/data-export',
      name: 'DataExport',
      component: () => import('../views/DataExportView.vue')
    }
  ]
});

export default router;
