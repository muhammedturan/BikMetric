import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface SavedRule {
  id: string
  name: string
  tableName: string
  filters: { column: string; operator: string; value: string }[]
  createdAt: string
}

export const useRuleStore = defineStore('rules', () => {
  const rules = ref<SavedRule[]>([])

  function loadRules() {
    const saved = localStorage.getItem('bikmetric_rules')
    if (saved) {
      rules.value = JSON.parse(saved)
    }
  }

  function saveRule(rule: Omit<SavedRule, 'id' | 'createdAt'>) {
    const newRule: SavedRule = {
      ...rule,
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
    }
    rules.value.push(newRule)
    localStorage.setItem('bikmetric_rules', JSON.stringify(rules.value))
    return newRule
  }

  function deleteRule(id: string) {
    rules.value = rules.value.filter((r) => r.id !== id)
    localStorage.setItem('bikmetric_rules', JSON.stringify(rules.value))
  }

  return { rules, loadRules, saveRule, deleteRule }
})
