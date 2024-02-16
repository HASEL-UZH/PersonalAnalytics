import { createRouter, createWebHashHistory } from 'vue-router';

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/experience-sampling',
      name: 'ExperienceSampling',
      component: () => import('../views/ExperienceSampling.vue')
    }
  ]
});

export default router;
