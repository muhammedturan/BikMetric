<template>
  <div class="rules-page">
    <h1 class="page-title">Kural Olusturucu</h1>

    <!-- Mode Toggle -->
    <div class="mode-toggle">
      <button class="toggle-btn" :class="{ active: mode === 'manual' }" @click="mode = 'manual'">
        Manuel
      </button>
      <button class="toggle-btn" :class="{ active: mode === 'ai' }" @click="mode = 'ai'">
        AI ile Olustur
      </button>
    </div>

    <!-- AI Mode Input -->
    <div class="card" v-if="mode === 'ai'">
      <div class="card-header">
        <h2>AI ile Kural Olustur</h2>
      </div>
      <div class="card-body">
        <div class="ai-input-wrapper">
          <textarea
            v-model="aiQuestion"
            class="ai-input"
            placeholder="Orn: Aylik ziyaretcisi 1 milyondan fazla olan haber siteleri"
            rows="3"
            @keydown.ctrl.enter="generateRulesFromAi"
          />
          <button class="btn btn-primary btn-lg" @click="generateRulesFromAi" :disabled="aiLoading || !aiQuestion.trim()">
            {{ aiLoading ? 'AI dusunuyor...' : 'Kural Olustur' }}
          </button>
        </div>
        <div class="examples">
          <span class="examples-label">Ornekler:</span>
          <button v-for="ex in aiExamples" :key="ex" class="example-chip" @click="aiQuestion = ex">
            {{ ex }}
          </button>
        </div>
        <div v-if="aiExplanation" class="explanation">
          {{ aiExplanation }}
        </div>
      </div>
    </div>

    <!-- Table Selection (only in manual mode) -->
    <div class="card" v-if="mode === 'manual'">
      <div class="card-header">
        <h2>Tablo Secimi</h2>
      </div>
      <div class="card-body">
        <select v-model="selectedTable" class="select" @change="onTableChange">
          <option value="">Tablo secin...</option>
          <option v-for="t in tables" :key="t.tableName" :value="t.tableName">
            {{ t.displayName }}
          </option>
        </select>
      </div>
    </div>

    <!-- AI Generated Rules Preview (only in AI mode, after generation) -->
    <div class="card" v-if="mode === 'ai' && selectedTable">
      <div class="card-header">
        <h2>AI Tarafindan Olusturulan Kurallar</h2>
        <span class="table-badge">{{ selectedTableDisplay }}</span>
      </div>
      <div class="card-body">
        <div v-for="(f, i) in filters" :key="i" class="filter-row">
          <select v-model="f.column" class="select">
            <option value="">Kolon...</option>
            <option v-for="col in currentColumns" :key="col.name" :value="col.name">
              {{ col.displayName }}
            </option>
          </select>
          <select v-model="f.operator" class="select">
            <option v-for="op in getOperators(f.column)" :key="op.value" :value="op.value">
              {{ op.label }}
            </option>
          </select>
          <input v-model="f.value" class="input" placeholder="Deger..." />
          <button class="btn btn-danger btn-sm" @click="removeFilter(i)">X</button>
        </div>
        <button class="btn btn-sm" @click="addFilter" style="margin-top: 8px">+ Kosul Ekle</button>

        <div class="actions">
          <button class="btn btn-primary" @click="executeQuery" :disabled="loading">
            {{ loading ? 'Sorgulaniyor...' : 'Calistir' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Filters (only in manual mode) -->
    <div class="card" v-if="mode === 'manual' && selectedTable">
      <div class="card-header">
        <h2>Filtreler</h2>
        <button class="btn btn-sm" @click="addFilter">+ Kosul Ekle</button>
      </div>
      <div class="card-body">
        <div v-if="filters.length === 0" class="empty-state">
          Henuz filtre eklenmedi. Tum veriler getirilecek.
        </div>
        <div v-for="(f, i) in filters" :key="i" class="filter-row">
          <select v-model="f.column" class="select">
            <option value="">Kolon...</option>
            <option v-for="col in currentColumns" :key="col.name" :value="col.name">
              {{ col.displayName }}
            </option>
          </select>
          <select v-model="f.operator" class="select">
            <option v-for="op in getOperators(f.column)" :key="op.value" :value="op.value">
              {{ op.label }}
            </option>
          </select>
          <input v-model="f.value" class="input" placeholder="Deger..." />
          <button class="btn btn-danger btn-sm" @click="removeFilter(i)">X</button>
        </div>

        <div class="actions">
          <button class="btn btn-primary" @click="executeQuery" :disabled="loading">
            {{ loading ? 'Sorgulaniyor...' : 'Calistir' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Results -->
    <div class="card" v-if="result">
      <div class="card-header">
        <h2>Sonuclar ({{ result.totalCount }} kayit)</h2>
        <button class="btn btn-sm" @click="showSaveDialog = true">Kurali Kaydet</button>
      </div>
      <div class="card-body table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th v-for="col in result.columns" :key="col">{{ col }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, i) in result.rows" :key="i">
              <td v-for="col in result.columns" :key="col">{{ row[col] }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Saved Rules -->
    <div class="card" v-if="ruleStore.rules.length">
      <div class="card-header">
        <h2>Kayitli Kurallar</h2>
      </div>
      <div class="card-body">
        <div v-for="rule in ruleStore.rules" :key="rule.id" class="saved-rule">
          <div>
            <strong>{{ rule.name }}</strong>
            <span class="rule-meta">{{ rule.tableName }} - {{ rule.filters.length }} filtre</span>
          </div>
          <div class="saved-rule-actions">
            <button class="btn btn-sm" @click="loadRule(rule)">Yukle</button>
            <button class="btn btn-sm btn-danger" @click="ruleStore.deleteRule(rule.id)">Sil</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Save Dialog -->
    <div v-if="showSaveDialog" class="overlay" @click.self="showSaveDialog = false">
      <div class="dialog">
        <h3>Kurali Kaydet</h3>
        <input v-model="ruleName" class="input" placeholder="Kural adi..." />
        <div class="dialog-actions">
          <button class="btn" @click="showSaveDialog = false">Iptal</button>
          <button class="btn btn-primary" @click="saveCurrentRule">Kaydet</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import api from '../../services/api'
import { useRuleStore, type SavedRule } from '../../stores/ruleStore'

const ruleStore = useRuleStore()

const tables = ref<any[]>([])
const selectedTable = ref('')
const filters = ref<{ column: string; operator: string; value: string }[]>([])
const result = ref<any>(null)
const loading = ref(false)
const showSaveDialog = ref(false)
const ruleName = ref('')

// AI mode state
const mode = ref<'manual' | 'ai'>('manual')
const aiQuestion = ref('')
const aiLoading = ref(false)
const aiExplanation = ref('')

const aiExamples = [
  'Aylik ziyaretcisi 1 milyondan fazla olan haber siteleri',
  'Teknoloji kategorisindeki haberler',
  '500 kelimeden fazla ve gorsel iceren haberler',
  'Goruntulenmesi 10000\'den fazla olan istatistikler',
]

const currentColumns = computed(() => {
  const t = tables.value.find((t) => t.tableName === selectedTable.value)
  return t?.columns || []
})

const selectedTableDisplay = computed(() => {
  const t = tables.value.find((t) => t.tableName === selectedTable.value)
  return t?.displayName || selectedTable.value
})

const operators = {
  string: [
    { value: 'equals', label: 'Esittir' },
    { value: 'notEquals', label: 'Esit Degil' },
    { value: 'contains', label: 'Icerir' },
    { value: 'startsWith', label: 'Ile Baslar' },
  ],
  number: [
    { value: 'equals', label: 'Esittir' },
    { value: 'notEquals', label: 'Esit Degil' },
    { value: 'greaterThan', label: 'Buyuktur' },
    { value: 'greaterThanOrEqual', label: 'Buyuk Esit' },
    { value: 'lessThan', label: 'Kucuktur' },
    { value: 'lessThanOrEqual', label: 'Kucuk Esit' },
  ],
  date: [
    { value: 'equals', label: 'Esittir' },
    { value: 'greaterThan', label: 'Sonra' },
    { value: 'lessThan', label: 'Once' },
  ],
}

function getOperators(columnName: string) {
  const col = currentColumns.value.find((c: any) => c.name === columnName)
  const type = col?.type || 'string'
  return operators[type as keyof typeof operators] || operators.string
}

function onTableChange() {
  filters.value = []
  result.value = null
}

function addFilter() {
  filters.value.push({ column: '', operator: 'equals', value: '' })
}

function removeFilter(index: number) {
  filters.value.splice(index, 1)
}

async function executeQuery() {
  loading.value = true
  try {
    const validFilters = filters.value.filter((f) => f.column && f.value)
    const { data } = await api.post('/query/execute', {
      tableName: selectedTable.value,
      filters: validFilters,
    })
    result.value = data
  } catch (e: any) {
    alert(e.response?.data?.message || 'Sorgu hatasi')
  } finally {
    loading.value = false
  }
}

async function generateRulesFromAi() {
  if (!aiQuestion.value.trim()) return
  aiLoading.value = true
  aiExplanation.value = ''

  try {
    const { data } = await api.post('/ai/generate-rules', { question: aiQuestion.value })
    selectedTable.value = data.tableName
    filters.value = data.filters.map((f: any) => ({
      column: f.column,
      operator: f.operator,
      value: f.value,
    }))
    aiExplanation.value = data.explanation
    result.value = null
  } catch (e: any) {
    alert(e.response?.data?.message || 'AI kural uretimi basarisiz oldu')
  } finally {
    aiLoading.value = false
  }
}

function saveCurrentRule() {
  if (!ruleName.value.trim()) return
  ruleStore.saveRule({
    name: ruleName.value,
    tableName: selectedTable.value,
    filters: filters.value.filter((f) => f.column && f.value),
  })
  showSaveDialog.value = false
  ruleName.value = ''
}

function loadRule(rule: SavedRule) {
  selectedTable.value = rule.tableName
  filters.value = [...rule.filters]
  result.value = null
}

onMounted(async () => {
  ruleStore.loadRules()
  try {
    const { data } = await api.get('/metadata/tables')
    tables.value = data
  } catch (e) {
    console.error('Metadata yuklenemedi', e)
  }
})
</script>

<style scoped>
.page-title { font-size: 24px; font-weight: 700; margin-bottom: 24px; }

.mode-toggle {
  display: flex;
  gap: 4px;
  background: var(--gray-100);
  border-radius: var(--radius);
  padding: 4px;
  width: fit-content;
  margin-bottom: 20px;
}
.toggle-btn {
  padding: 8px 20px;
  border-radius: var(--radius);
  font-size: 13px;
  font-weight: 500;
  background: transparent;
  color: var(--gray-500);
  border: none;
  cursor: pointer;
  transition: all 0.15s;
}
.toggle-btn.active {
  background: white;
  color: var(--gray-800);
  box-shadow: var(--shadow);
}

.ai-input-wrapper { display: flex; gap: 12px; align-items: flex-end; margin-bottom: 16px; }
.ai-input {
  flex: 1; padding: 12px 16px; border: 2px solid var(--gray-200);
  border-radius: var(--radius-lg); font-size: 15px; resize: none;
  font-family: var(--font); line-height: 1.5;
}
.ai-input:focus { border-color: var(--primary); outline: none; box-shadow: 0 0 0 3px var(--primary-light); }
.btn-lg { padding: 12px 24px; font-size: 14px; border-radius: var(--radius-lg); white-space: nowrap; }

.examples { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.examples-label { font-size: 12px; color: var(--gray-400); font-weight: 500; }
.example-chip {
  padding: 4px 12px; border-radius: 20px; background: var(--gray-100);
  font-size: 12px; color: var(--gray-600); border: none; cursor: pointer;
  transition: all 0.15s;
}
.example-chip:hover { background: var(--primary-light); color: var(--primary); }

.explanation {
  margin-top: 12px; color: var(--gray-600); font-size: 14px;
  padding: 12px; background: var(--gray-50); border-radius: var(--radius);
  line-height: 1.5;
}

.table-badge {
  padding: 4px 12px; border-radius: 20px; background: var(--primary-light);
  color: var(--primary); font-size: 12px; font-weight: 600;
}

.card {
  background: white; border-radius: var(--radius-lg);
  box-shadow: var(--shadow); margin-bottom: 16px;
}
.card-header {
  padding: 16px 20px; border-bottom: 1px solid var(--gray-100);
  display: flex; align-items: center; justify-content: space-between;
}
.card-header h2 { font-size: 16px; font-weight: 600; }
.card-body { padding: 20px; }

.select {
  padding: 8px 12px; border: 1px solid var(--gray-200); border-radius: var(--radius);
  font-size: 14px; background: white; min-width: 140px;
}
.select:focus { border-color: var(--primary); }

.input {
  padding: 8px 12px; border: 1px solid var(--gray-200); border-radius: var(--radius);
  font-size: 14px; flex: 1;
}
.input:focus { border-color: var(--primary); }

.filter-row { display: flex; gap: 8px; align-items: center; margin-bottom: 8px; }

.empty-state { color: var(--gray-400); font-size: 14px; padding: 8px 0; }

.actions { margin-top: 16px; display: flex; gap: 8px; }

.btn {
  padding: 8px 16px; border-radius: var(--radius); font-size: 13px;
  font-weight: 500; background: var(--gray-100); color: var(--gray-700);
  border: none; cursor: pointer; transition: all 0.15s;
}
.btn:hover { background: var(--gray-200); }
.btn-primary { background: var(--primary); color: white; }
.btn-primary:hover:not(:disabled) { background: var(--primary-hover); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-danger { background: #fef2f2; color: var(--danger); }
.btn-danger:hover { background: #fee2e2; }
.btn-sm { padding: 6px 12px; font-size: 12px; }

.table-wrapper { overflow-x: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.data-table th {
  text-align: left; padding: 10px 12px; background: var(--gray-50);
  border-bottom: 2px solid var(--gray-200); font-weight: 600; white-space: nowrap;
}
.data-table td {
  padding: 8px 12px; border-bottom: 1px solid var(--gray-100); white-space: nowrap;
}
.data-table tr:hover td { background: var(--gray-50); }

.saved-rule {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 0; border-bottom: 1px solid var(--gray-100);
}
.saved-rule:last-child { border-bottom: none; }
.rule-meta { font-size: 12px; color: var(--gray-400); margin-left: 8px; }
.saved-rule-actions { display: flex; gap: 8px; }

.overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.4);
  display: flex; align-items: center; justify-content: center; z-index: 100;
}
.dialog {
  background: white; border-radius: var(--radius-lg); padding: 24px;
  width: 400px; box-shadow: var(--shadow-lg);
}
.dialog h3 { font-size: 18px; margin-bottom: 16px; }
.dialog .input { width: 100%; margin-bottom: 16px; }
.dialog-actions { display: flex; gap: 8px; justify-content: flex-end; }
</style>
