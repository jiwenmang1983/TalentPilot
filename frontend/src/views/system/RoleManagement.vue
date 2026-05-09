<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>🛡 角色管理</h1>
      <p>系统角色与权限配置</p>
    </div>
  </div>

  <div class="role-management">
    <div class="toolbar">
      <a-button type="primary" @click="openDrawer()">
        <template #icon><PlusOutlined /></template>
        新建角色
      </a-button>
    </div>

    <a-table
      :columns="columns"
      :data-source="roles"
      :loading="loading"
      row-key="id"
      :expandable="{ expandedRowKeys: expandedRowKeys, onExpand: handleExpand }"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'isSystem'">
          <a-tag :color="record.isSystem ? 'blue' : 'default'">
            {{ record.isSystem ? '系统角色' : '自定义' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-button type="link" size="small" @click="openDrawer(record)">
              编辑
            </a-button>
            <a-button
              type="link"
              size="small"
              danger
              :disabled="record.isSystem"
              @click="handleDelete(record)"
            >
              删除
            </a-button>
          </a-space>
        </template>
        <template v-else-if="column.key === 'expandedRowRender'">
          <div v-if="expandedRowKeys.includes(record.id)" class="permission-config">
            <div class="permission-header">
              <span>权限配置</span>
              <a-button type="primary" size="small" @click="savePermissions(record)">
                保存权限
              </a-button>
            </div>
            <a-tree
              v-model:checkedKeys="selectedPermissions[record.id]"
              :tree-data="permissionTree"
              checkable
              :selectable="false"
            />
          </div>
        </template>
      </template>
    </a-table>

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
        <a-form-item label="角色名称" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入角色名称" />
        </a-form-item>
        <a-form-item label="角色代码" name="code">
          <a-input v-model:value="formState.code" placeholder="请输入角色代码" :disabled="isEdit" />
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
import { ref, reactive, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import { roleApi, permissionApi } from '@/api'
import dayjs from 'dayjs'

const roles = ref([])
const permissions = ref([])
const permissionTree = ref([])
const loading = ref(false)
const drawerVisible = ref(false)
const drawerTitle = ref('新建角色')
const isEdit = ref(false)
const formRef = ref(null)
const submitLoading = ref(false)

const expandedRowKeys = ref([])
const selectedPermissions = reactive({})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
  { title: '角色名称', dataIndex: 'roleName', key: 'roleName' },
  { title: '角色代码', dataIndex: 'roleKey', key: 'roleKey' },
  { title: '描述', dataIndex: 'description', key: 'description' },
  { title: '类型', key: 'isSystem', width: 100 },
  { title: '创建时间', key: 'createdAt', customRender: ({ text }) => text ? dayjs(text).format('YYYY-MM-DD HH:mm') : '-' },
  { title: '操作', key: 'actions', width: 150, fixed: 'right' }
]

const formState = reactive({
  id: null,
  name: '',
  code: '',
  description: ''
})

const formRules = {
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  code: [
    { required: true, message: '请输入角色代码', trigger: 'blur' },
    { pattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/, message: '角色代码只能包含字母、数字和下划线', trigger: 'blur' }
  ]
}

function transformPermissionsToTree(perms) {
  const map = {}
  const roots = []

  perms.forEach(p => {
    map[p.id] = { key: p.id, title: p.name, children: [] }
  })

  perms.forEach(p => {
    if (p.parentId && map[p.parentId]) {
      map[p.parentId].children.push(map[p.id])
    } else {
      roots.push(map[p.id])
    }
  })

  return roots
}

async function fetchRoles() {
  loading.value = true
  try {
    const res = await roleApi.list()
    roles.value = res.data || res || []
  } catch (e) {
    message.error('获取角色列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchPermissions() {
  try {
    const res = await permissionApi.getAll()
    permissions.value = res.data || res || []
    permissionTree.value = transformPermissionsToTree(permissions.value)
  } catch (e) {
    console.error('Failed to fetch permissions:', e)
  }
}

async function fetchRolePermissions(roleId) {
  try {
    const res = await roleApi.getPermissions(roleId)
    const permIds = res.data || res || []
    selectedPermissions[roleId] = permIds
  } catch (e) {
    console.error('Failed to fetch role permissions:', e)
    selectedPermissions[roleId] = []
  }
}

function handleExpand(expanded, record) {
  if (expanded && !selectedPermissions[record.id]) {
    fetchRolePermissions(record.id)
  }
  expandedRowKeys.value = expanded ? [record.id] : []
}

function openDrawer(record = null) {
  if (record) {
    isEdit.value = true
    drawerTitle.value = '编辑角色'
    formState.id = record.id
    formState.name = record.roleName
    formState.code = record.roleKey
    formState.description = record.description
  } else {
    isEdit.value = false
    drawerTitle.value = '新建角色'
    Object.assign(formState, { id: null, name: '', code: '', description: '' })
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

    if (isEdit.value) {
      await roleApi.update(formState.id, {
        roleName: formState.name,       // name → roleName
        description: formState.description
      })
      message.success('更新成功')
    } else {
      await roleApi.create({
        roleName: formState.name,       // name → roleName
        roleKey: formState.code,        // code → roleKey
        description: formState.description
      })
      message.success('创建成功')
    }

    closeDrawer()
    fetchRoles()
  } catch (e) {
    if (e?.message && !e?.response?.data?.message) {
      message.error(e.message)
    }
  } finally {
    submitLoading.value = false
  }
}

async function handleDelete(record) {
  Modal.confirm({
    title: '确认删除',
    content: `确定要删除角色 ${record.roleName} 吗？`,
    onOk: async () => {
      try {
        await roleApi.delete(record.id)
        message.success('删除成功')
        fetchRoles()
      } catch (e) {
        message.error('删除失败')
      }
    }
  })
}

async function savePermissions(record) {
  try {
    const permIds = selectedPermissions[record.id] || []
    await roleApi.updatePermissions(record.id, permIds)
    message.success('权限保存成功')
  } catch (e) {
    message.error('保存权限失败')
  }
}

onMounted(() => {
  fetchRoles()
  fetchPermissions()
})
</script>

<style scoped>
.role-management {
  height: 100%;
}

.toolbar {
  margin-bottom: 16px;
}

.permission-config {
  padding: 16px;
  background: #fafafa;
  border-radius: 4px;
}

.permission-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  font-weight: 500;
}
</style>