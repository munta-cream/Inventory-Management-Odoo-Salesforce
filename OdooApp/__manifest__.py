{
    'name': 'Inventory Aggregator',
    'version': '1.0',
    'category': 'Tools',
    'summary': 'Import and display aggregated data from external inventory management system',
    'description': 'This module allows importing aggregated data from an external API and displaying it in Odoo.',
    'author': 'Your Name',
    'website': 'https://www.example.com',
    'depends': ['base'],
    'data': [
        'security/ir.model.access.csv',
        'views/template_views.xml',
        'views/menu.xml',
    ],
    'installable': True,
    'application': True,
}
