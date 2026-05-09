<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>🏢 部门管理</h1>
      <p>企业组织架构树形管理</p>
    </div>
  </div>

  <div class="department-tree">
    <div class="toolbar">
      <a-button type="primary" @click="openDrawer('create')">
        <template #icon><PlusOutlined /></template>
        新建部门
      </a-button>
    </div>

    <a-tree
      :tree-data="treeData"
      :show-line="true"
      :selectedKeys="selectedKeys"
      @select="handleSelect"
      @contextmenu="handleContextMenu"
      draggable
      @drop="onDrop"
    >
      <template #title="{ key, title }">
        <div class="tree-node" @contextmenu.prevent="showContextMenu($event, key)">
          <span>{{ title }}</span>
        </div>
      </template>
    </a-tree>

    <div v-if="contextMenuVisible" class="context-menu" :style="contextMenuStyle">
      <a-button type="link" size="small" @click="openDrawer('createChild', contextMenuId)">
        <PlusOutlined /> 添加子部门
      </a-button>
      <a-button type="link" size="small" @click="openDrawer('edit', contextMenuId)">
        <EditOutlined /> 编辑
      </a-button>
      <a-button type="link" size="small" danger @click="handleDelete(contextMenuId)">
        <DeleteOutlined /> 删除
      </a-button>
    </div>

    <a-drawer
      v-model:open="drawerVisible"
      :title="drawerTitle"
      width="400"
      @close="closeDrawer"
    >
      <a-form
        :model="formState"
        :rules="formRules"
        ref="formRef"
        layout="vertical"
      >
        <a-form-item label="部门名称" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入部门名称" />
        </a-form-item>
        <a-form-item label="部门代码" name="code">
          <a-input v-model:value="formState.code" placeholder="请输入部门代码" />
        </a-form-item>
        <a-form-item label="上级部门" name="parentId">
          <a-tree-select
            v-model:value="formState.parentId"
            :tree-data="treeData"
            placeholder="请选择上级部门"
            tree-default-expand-all
            :dropdown-match-select-width="false"
          />
        </a-form-item>
        <a-form-item label="描述" name="description">
          <a-textarea v-model:value="formState.description" placeholder="请输入描述" :rows="3" />
        </a-form-item>
      </a-form>
      <template #footer>
        <a-space>
          <a-button @click="closeDrawer">取消</a-button>
          <a-button type="primary" :loading="submitLoading" @click="handleSubmit">
            确定
          </a-button>
        </a-space>
      </template>
    </a-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import { departmentApi } from '@/api'

const treeData = ref([])
const selectedKeys = ref([])
const loading = ref(false)

const drawerVisible = ref(false)
const drawerTitle = ref('新建部门')
const formRef = ref(null)
const submitLoading = ref(false)
const drawerMode = ref('create')
const currentDepartmentId = ref(null)

const contextMenuVisible = ref(false)
const contextMenuStyle = reactive({ left: '0px', top: '0px' })
const contextMenuId = ref(null)

const formState = reactive({
  id: null,
  name: '',
  code: '',
  parentId: null,
  description: ''
})

const formRules = {
  name: [{ required: true, message: '请输入部门名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入部门代码', trigger: 'blur' }]
}

function transformToTree(nodes, excludeId = null) {
  if (!nodes) return []
  return nodes
    .filter(n => n.id !== excludeId)
    .map(node => ({
      key: node.id,
      title: node.departmentName,   // API返回departmentName
      children: transformToTree(node.children, excludeId)
    }))
}

async function fetchDepartments() {
  loading.value = true
  try {
    const res = await departmentApi.getTree()
    const rawData = res.data || res || []
    treeData.value = transformToTree(rawData)
  } catch (e) {
    message.error('获取部门列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchDepartmentById(id) {
  try {
    const res = await departmentApi.getById(id)
    return res.data || res
  } catch (e) {
    return null
  }
}

function handleSelect(keys) {
  selectedKeys.value = keys
}

function handleContextMenu(info) {
  // handled by custom context menu
}

function showContextMenu(event, key) {
  contextMenuId.value = key
  contextMenuStyle.left = `${event.clientX}px`
  contextMenuStyle.top = `${event.clientY}px`
  contextMenuVisible.value = true
}

function hideContextMenu() {
  contextMenuVisible.value = false
}

function openDrawer(mode, deptId = null) {
  drawerMode.value = mode
  currentDepartmentId.value = deptId
  contextMenuVisible.value = false

  if (mode === 'edit' && deptId) {
    drawerTitle.value = '编辑部门'
    fetchDepartmentById(deptId).then(dept => {
      if (dept) {
        formState.id = dept.id
        formState.name = dept.departmentName   // API返回departmentName/departmentKey
        formState.code = dept.departmentKey
        formState.parentId = dept.parentId
        formState.description = dept.description || ''
      }
    })
  } else if (mode === 'createChild' && deptId) {
    drawerTitle.value = '添加子部门'
    Object.assign(formState, { id: null, name: '', code: '', parentId: deptId, description: '' })
  } else {
    drawerTitle.value = '新建部门'
    Object.assign(formState, { id: null, name: '', code: '', parentId: null, description: '' })
  }

  drawerVisible.value = true
}

function closeDrawer() {
  drawerVisible.value = false
  formRef.value?.resetFields()
}

async function handleSubmit() {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    if (drawerMode.value === 'edit' && formState.id) {
      await departmentApi.update(formState.id, {
        departmentName: formState.name,  // name → departmentName
        sortOrder: 0
      })
      message.success('更新成功')
    } else {
      await departmentApi.create({
        departmentName: formState.name,  // name → departmentName
        departmentKey: formState.code,   // code → departmentKey
        parentId: formState.parentId ?? null,
        sortOrder: 0
      })
      message.success('创建成功')
    }

    closeDrawer()
    fetchDepartments()
  } catch (e) {
    if (e?.message && !e?.response?.data?.message) {
      message.error(e.message)
    }
  } finally {
    submitLoading.value = false
  }
}

async function handleDelete(id) {
  contextMenuVisible.value = false
  Modal.confirm({
    title: '确认删除',
    content: '确定要删除该部门吗？',
    onOk: async () => {
      try {
        await departmentApi.delete(id)
        message.success('删除成功')
        fetchDepartments()
      } catch (e) {
        message.error('删除失败')
      }
    }
  })
}

async function onDrop(info) {
  const dragKey = info.dragNode.key
  const dropKey = info.node.key
  const pos = info.node.pos.split('-')
  const dropPos = pos.length - 2

  if (info.dropToGap) {
    const parentId = dropPos > 0 ? parseInt(pos[dropPos]) : null
    try {
      await departmentApi.update(dragKey, { parentId })
      message.success('移动成功')
      fetchDepartments()
    } catch (e) {
      message.error('移动失败')
    }
  }
}

onMounted(() => {
  fetchDepartments()
  document.addEventListener('click', hideContextMenu)
})

onUnmounted(() => {
  document.removeEventListener('click', hideContextMenu)
})
</script>

<style scoped>
.department-tree {
  height: 100%;
}

.toolbar {
  margin-bottom: 16px;
}

.tree-node {
  display: inline-flex;
  align-items: center;
  padding: 4px 8px;
  border-radius: 4px;
}

.tree-node:hover {
  background: #f0f0f0;
}

.context-menu {
  position: fixed;
  background: white;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  padding: 4px;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
</style>