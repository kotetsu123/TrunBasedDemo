"""Start a dedicated modeling session without replacing an existing Blender session."""
import bpy
import addon_utils

addon_utils.enable('blender_mcp', default_set=False, persistent=True)
bpy.context.preferences.addons['blender_mcp'].preferences.telemetry_consent = False
bpy.context.scene.name = 'VolcanoCave_Workspace'
bpy.context.scene.blendermcp_auto_start_server = True
if not getattr(bpy.types, 'blendermcp_server', None):
    bpy.ops.blendermcp.start_server()
