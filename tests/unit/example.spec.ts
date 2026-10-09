import { mount } from '@vue/test-utils'
import HomePage from '@/views/HomePage.vue'
import { describe, expect, test } from 'vitest'

describe('HomePage.vue', () => {
  test('renders home vue', () => {
    const wrapper = mount(HomePage)
    expect(wrapper.text()).toContain('App Multi-tenant')
    expect(wrapper.text()).toContain('Tiendas disponibles')
  })
})
